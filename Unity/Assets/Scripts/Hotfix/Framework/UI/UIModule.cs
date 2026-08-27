using System;
using FairyGUI;
using UnityEngine;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Event;
using System.Collections.Generic;
using Hotfix.Framework.ObjectPool;
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
                Recycle(ui);
            }

            // 驱动界面组帧更新
            foreach (var (_, group) in m_UIGroupDict)
            {
                if (group.Pause) continue;
                group.OnUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            SafeAreaHelper.OnSafeAreaChanged -= ApplyGRootSafeArea;

            m_UIGroupDict.Clear();
            m_LoadingDict.Clear();

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