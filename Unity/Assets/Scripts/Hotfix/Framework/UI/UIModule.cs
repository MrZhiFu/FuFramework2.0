using System;
using FairyGUI;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;
using FuFramework.ObjectPool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// UI管理模块。
    /// 目标：用于管理所有UI界面的加载，关闭，释放等操作。
    /// </summary>
    public sealed partial class UIModule : ModuleBase
    {
        /// <summary>
        /// 正在加载中的界面字典, key为界面Id, value为界面名称
        /// </summary>
        private Dictionary<int, string> m_LoadingDict;

        /// <summary>
        /// 关闭后待回收的界面集合
        /// </summary>
        private Queue<ViewBase> m_WaitRecycleQueue;

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
        private ObjectPoolModule.ObjectPool<ViewObject> m_InstancePool;

        /// <summary>
        /// FGui的包管理器
        /// </summary>
        public FuiPkgManager PkgManager { get; private set; }

        /// <summary>
        /// 界面自增序列号，每打开一个界面就加1
        /// </summary>
        private int m_SerialId;


        /// <summary>
        /// 界面实例对象池自动释放可释放对象的间隔秒数
        /// </summary>
        private const float UIInstanceAutoReleaseInterval = 60f;

        /// <summary>
        /// 界面实例对象池的容量
        /// </summary>
        private const int UIInstancePoolCapacity = 16;

        /// <summary>
        /// 界面实例对象池对象过期秒数
        /// </summary>
        private const float UIInstanceExpireTime = 60f;

        /// <summary>
        /// 获取或设置界面实例对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public float InstanceAutoReleaseInterval
        {
            get => m_InstancePool.AutoReleaseInterval;
            set => m_InstancePool.AutoReleaseInterval = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池的容量。
        /// </summary>
        public int InstanceCapacity
        {
            get => m_InstancePool.Capacity;
            set => m_InstancePool.Capacity = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池对象过期秒数。
        /// </summary>
        public float InstanceExpireTime
        {
            get => m_InstancePool.ExpireTime;
            set => m_InstancePool.ExpireTime = value;
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            m_UIGroupDict      = new Dictionary<EUILayer, UIGroup>();
            m_LoadingDict      = new Dictionary<int, string>();
            m_WaitRecycleQueue = new Queue<ViewBase>();

            m_ObjectPoolModule = ModuleManager.GetModule<ObjectPoolModule>();
            m_InstancePool     = m_ObjectPoolModule.CreateObjectPool<ViewObject>("UIInstanceObjectPool");

            m_EventModule = ModuleManager.GetModule<EventModule>();
            PkgManager    = new FuiPkgManager();

            m_SerialId = 0;

            InstanceAutoReleaseInterval = UIInstanceAutoReleaseInterval;
            InstanceCapacity            = UIInstancePoolCapacity;
            InstanceExpireTime          = UIInstanceExpireTime;

            // 刘海屏适配：初始化安全区数据，并将 GRoot 移动到安全区内
            SafeAreaHelper.Refresh();
            ApplyGRootSafeArea();

            // 监听安全区变化（方向切换等），重新应用 GRoot 配置
            SafeAreaHelper.OnSafeAreaChanged += ApplyGRootSafeArea;

            // 遍历所有UI层级，并添加UI组
            foreach (EUILayer layer in Enum.GetValues(typeof(EUILayer)))
            {
                if (AddUIGroup(layer)) continue;
                FuLogger.LogError($"[UIModule] 添加UI组 '{layer.ToString()}' 失败 .");
            }
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
                RecycleUI(ui);
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
            m_WaitRecycleQueue.Clear();
            PkgManager.ReleaseAll();
        }

        /// <summary>
        /// 将 GRoot 移动到安全区内。普通 UI 自动适配；全屏 UI 通过负偏移覆盖刘海。
        /// </summary>
        private static void ApplyGRootSafeArea()
        {
            GRoot.inst.SetSize(SafeAreaHelper.SafeWidth, SafeAreaHelper.SafeHeight);
            GRoot.inst.SetXY(SafeAreaHelper.OffsetX, SafeAreaHelper.OffsetY);
        }
    }
}