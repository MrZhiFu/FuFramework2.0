using System;
using FairyGUI;
using UnityEngine;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Event;
using Hotfix.Framework.ObjectPool;
using System.Collections.Generic;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI管理模块。
    /// 目标：用于管理所有UI界面的加载，关闭，释放等操作。
    /// </summary>
    public sealed partial class UIModule : ModuleBase, ICancelAsync
    {
        /// <summary>
        /// 事件组件
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// 对象池管理模块
        /// </summary>
        private ObjectPoolModule m_ObjectPoolModule;

        /// <summary>
        /// 界面实例对象池
        /// </summary>
        private ObjectPool<WinObject> m_WinObjPool;

        /// <summary>
        /// FGui的包管理器
        /// </summary>
        public FuiPkgManager PkgManager { get; private set; }


        /// <summary>
        /// 正在加载中的界面字典, key为界面Id, value为界面名称
        /// </summary>
        private Dictionary<int, string> m_LoadingDict;

        /// <summary>
        /// 关闭后待回收的界面集合
        /// </summary>
        private Queue<WinBase> m_WaitRecycleQueue;

        /// <summary>
        /// OnUpdate 专用的界面组快照列表。
        /// 不得与其它遍历共用：组内界面的 Update 回调是用户代码，可能触发模块级操作
        /// （AddGroup / OnDispose 会增删 m_UIGroupDict），共用缓存会导致正在遍历的列表被清空、
        /// 本帧其后的界面组不再更新。
        /// </summary>
        private readonly List<UIGroup> m_CachedUpdateGroupList = new();


        /// <summary>
        /// 界面自增序列号，每打开一个界面就加1
        /// </summary>
        private int m_SerialId;


        /// <summary>
        /// 界面实例对象池自动销毁检查的间隔秒数
        /// </summary>
        private const float DefaultAutoDisposeCheckInterval = 60f;

        /// <summary>
        /// 界面实例对象池的容量
        /// </summary>
        private const int DefaultPoolCapacity = 16;

        /// <summary>
        /// 界面实例对象池对象过期秒数
        /// </summary>
        private const float DefaultPoolExpireTimeAfterIdle = 60f;


        /// <summary>
        /// 获取或设置界面实例对象池自动销毁检查的间隔秒数。
        /// </summary>
        public float PoolAutoDisposeCheckInterval
        {
            get => m_WinObjPool.AutoDisposeCheckInterval;
            set => m_WinObjPool.AutoDisposeCheckInterval = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池的容量。
        /// </summary>
        public int PoolCapacity
        {
            get => m_WinObjPool.Capacity;
            set => m_WinObjPool.Capacity = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池对象过期秒数。
        /// 对象闲置（距上次使用或回收）超过该秒数即视为过期，纳入销毁候选。
        /// </summary>
        public float PoolExpireTimeAfterIdle
        {
            get => m_WinObjPool.ExpireTimeAfterIdle;
            set => m_WinObjPool.ExpireTimeAfterIdle = value;
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            m_UIGroupDict      = new Dictionary<EUILayer, UIGroup>();
            m_LoadingDict      = new Dictionary<int, string>();
            m_WaitRecycleQueue = new Queue<WinBase>();

            m_ObjectPoolModule = ModuleManager.GetModule<ObjectPoolModule>();
            m_WinObjPool       = m_ObjectPoolModule.CreateObjectPool<WinObject>("UIWinObjectPool");

            m_EventModule = ModuleManager.GetModule<EventModule>();
            PkgManager    = new FuiPkgManager();

            m_SerialId = 0;

            PoolAutoDisposeCheckInterval = DefaultAutoDisposeCheckInterval;
            PoolCapacity                 = DefaultPoolCapacity;
            PoolExpireTimeAfterIdle      = DefaultPoolExpireTimeAfterIdle;

            // 刘海屏适配：初始化安全区数据，并将 GRoot 移动到安全区内
            SafeAreaHelper.Refresh();
            ApplyGRootSafeArea();

            // 监听安全区变化（方向切换等），重新应用 GRoot 配置
            SafeAreaHelper.OnSafeAreaChanged += ApplyGRootSafeArea;

            // 遍历所有UI层级，并添加UI组
            foreach (EUILayer layer in Enum.GetValues(typeof(EUILayer)))
            {
                if (AddGroup(layer)) continue;
                FuLogger.LogError($"[UIModule] 添加UI组 '{layer.ToString()}' 失败 .");
            }

            // 初始化 UI 背景模糊功能（挂载截屏组件 + 预热 Shader）
            InitBlur();
        }

        /// <summary>
        /// 帧更新。
        /// </summary>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 检测安全区变化（方向切换等）
            SafeAreaHelper.OnUpdate();

            // 回收等待回收的界面
            while (m_WaitRecycleQueue.Count > 0)
            {
                var ui = m_WaitRecycleQueue.Dequeue();

                // WinObject 对象池 Recycle 在池中找不到目标时会抛异常；若让其逃逸会中断 ModuleManager 本帧其后
                // 所有模块的 Update，且该 win 会因跳过回收而泄漏，故逐项 try/catch 兜底（与 EntityModule 一致）。
                try
                {
                    Recycle(ui);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[UIModule] 回收界面 '{ui?.WinName}' 出现异常: {e.Message}");
                }
            }

            // 驱动界面组帧更新。
            // 先快照到复用列表再遍历（与 ObjectPoolModule.OnUpdate 一致）：界面 Update 回调是用户代码，
            // 期间可能增删界面组（AddGroup / OnDispose 会改 m_UIGroupDict），直接枚举字典时枚举器一旦失效
            // 会从 ModuleManager.Update（无保护）逃逸并中断本帧其后所有模块。快照循环本身在回调之外的
            // 单线程路径上执行，不会被并发修改，无需额外保护。
            m_CachedUpdateGroupList.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                m_CachedUpdateGroupList.Add(group);
            }

            foreach (var group in m_CachedUpdateGroupList)
            {
                if (group == null || group.Pause) continue;

                // WinBase.OnUpdate 是用户代码，任一窗口抛异常都会从 ModuleManager.Update（无保护）逃逸，
                // 导致本帧其后所有模块停止更新，故逐组 try/catch 兜底（与上方回收循环 / EntityModule.OnUpdate 一致）。
                try
                {
                    group.OnUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[UIModule] 更新界面组 '{group.Layer.ToString()}' 出现异常: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            SafeAreaHelper.OnSafeAreaChanged -= ApplyGRootSafeArea;

            // 逐个回收各组内界面的 WinInfo：WinInfo 经引用池 Acquire，唯一回收点是 UIGroup.Remove。
            // 原先直接 m_UIGroupDict.Clear() 会丢弃这些 WinInfo，只要销毁时还有打开的界面就会造成引用池泄漏。
            // 复用同一列表避免按组多次分配；Remove 会同步出队，随后再整体清空字典。
            var groupWins = new List<WinBase>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                if (group == null) continue;

                group.GetAll(groupWins);
                for (var i = 0; i < groupWins.Count; i++)
                {
                    var win = groupWins[i];
                    if (win == null) continue;

                    try
                    {
                        // 与正常关闭一致：先出组（回收 WinInfo），再走窗口关闭回调。
                        group.Remove(win);
                        win._OnClose();
                    }
                    catch (Exception e)
                    {
                        FuLogger.LogWarning($"[UIModule] 释放时关闭界面 '{win.WinName}' 出现异常: {e.Message}");
                    }
                    finally
                    {
                        // 无论上一步是否异常，都必须把窗口交回待回收队列，由下方队列排空统一走
                        // Recycle（_OnRecycle + 归还对象池）。否则销毁时仍打开的窗口只回收了 WinInfo，
                        // 其 WinObject 仍处于使用中，会残留在池里直至 ObjectPoolModule 强制回收并打出
                        // 「仍有对象处于使用中」告警。
                        m_WaitRecycleQueue.Enqueue(win);
                    }
                }

                groupWins.Clear();
            }

            m_UIGroupDict.Clear();
            m_LoadingDict.Clear();

            // 清空快照列表：避免持有已销毁的界面组引用（与 ObjectPoolModule.OnDispose 清理缓存列表一致）
            m_CachedUpdateGroupList.Clear();

            // 清空回收队列中待回收的界面，避免 teardown 时丢弃未回收的 WinBase/对象池槽位
            while (m_WaitRecycleQueue.Count > 0)
            {
                var ui = m_WaitRecycleQueue.Dequeue();
                try
                {
                    Recycle(ui);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[UIModule] 释放时回收界面 '{ui?.WinName}' 出现异常: {e.Message}");
                }
            }

            PkgManager.RemoveAllPkg();
            ReleaseBlur();
        }

        /// <summary>
        /// 将 GRoot 缩放并移动到安全区内。
        /// </summary>
        private static void ApplyGRootSafeArea()
        {
            GRoot.inst.SetSize(SafeAreaHelper.SafeWidth, SafeAreaHelper.SafeHeight);
            GRoot.inst.SetXY(SafeAreaHelper.OffsetX, SafeAreaHelper.OffsetY);
        }
    }
}