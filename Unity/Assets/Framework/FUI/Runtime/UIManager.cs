//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Asset.Runtime;
using GameFrameX.ObjectPool;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    internal sealed partial class UIManager
    {
        private readonly Dictionary<int, string> m_LoadingDict; // 正在加载的界面集合, key为界面Id, value为界面名称
        private readonly HashSet<int> m_WaitReleaseSet; // 待释放的界面集合，int为界面Id
        private readonly Queue<IUIForm> m_WaitRecycleQueue; // 待回收的界面集合

        private IAssetManager m_AssetManager; // 资源管理器
        private IObjectPoolManager m_ObjectPoolManager; // 对象池管理器
        private FuiPackageComponent FuiPackage { get; set; } // fairyGUI包组件
        private IObjectPool<UIFormInstanceObject> m_InstancePool; // 界面实例对象池
        private IUIFormHelper m_UIFormHelper; // 界面辅助器

        private int m_Serial; // 界面序列号，没打开一个界面就加1
        private bool m_IsShutdown; // 是否是关机

        // private readonly LoadAssetCallbacks m_LoadAssetCallbacks;
        // private EventHandler<OpenUIFormUpdateEventArgs> m_OpenUIFormUpdateEventHandler;
        // private EventHandler<OpenUIFormDependencyAssetEventArgs> m_OpenUIFormDependencyAssetEventHandler;


        /// <summary>
        /// 初始化界面管理器的新实例。
        /// </summary>
        public UIManager()
        {
            m_UIGroupDict = new Dictionary<string, UIGroup>(StringComparer.Ordinal);
            m_LoadingDict = new Dictionary<int, string>();
            m_WaitReleaseSet = new HashSet<int>();
            m_WaitRecycleQueue = new Queue<IUIForm>();

            // m_LoadAssetCallbacks = new LoadAssetCallbacks(LoadAssetSuccessCallback, LoadAssetFailureCallback, LoadAssetUpdateCallback, LoadAssetDependencyAssetCallback);
            m_ObjectPoolManager = null;
            m_AssetManager = null;
            m_InstancePool = null;
            m_UIFormHelper = null;
            m_Serial = 0;
            m_IsShutdown = false;

            m_OpenUIFormSuccessEventHandler = null;
            m_OpenUIFormFailureEventHandler = null;
            // m_OpenUIFormUpdateEventHandler = null;
            // m_OpenUIFormDependencyAssetEventHandler = null;
            m_CloseUIFormCompleteEventHandler = null;
        }

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

        /*/// <summary>
        /// 获取或设置界面实例对象池的优先级。
        /// </summary>
        public int InstancePriority
        {
            get { return m_InstancePool.Priority; }
            set { m_InstancePool.Priority = value; }
        }*/

        /*
        /// <summary>
        /// 打开界面更新事件。
        /// </summary>
        public event EventHandler<OpenUIFormUpdateEventArgs> OpenUIFormUpdate
        {
            add { m_OpenUIFormUpdateEventHandler += value; }
            remove { m_OpenUIFormUpdateEventHandler -= value; }
        }

        /// <summary>
        /// 打开界面时加载依赖资源事件。
        /// </summary>
        public event EventHandler<OpenUIFormDependencyAssetEventArgs> OpenUIFormDependencyAsset
        {
            add { m_OpenUIFormDependencyAssetEventHandler += value; }
            remove { m_OpenUIFormDependencyAssetEventHandler -= value; }
        }*/

        /// <summary>
        /// 界面管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            while (m_WaitRecycleQueue.Count > 0)
            {
                var uiForm = m_WaitRecycleQueue.Dequeue();
                RecycleUIForm(uiForm);
            }

            foreach (var (_, group) in m_UIGroupDict)
            {
                group.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        protected override void Shutdown()
        {
            m_IsShutdown = true;
            CloseAllLoadedUIForms();
            m_UIGroupDict.Clear();
            m_LoadingDict.Clear();
            m_WaitReleaseSet.Clear();
            m_WaitRecycleQueue.Clear();
        }

        /// <summary>
        /// 设置对象池管理器。
        /// </summary>
        /// <param name="objectPoolManager">对象池管理器。</param>
        public void SetObjectPoolManager(IObjectPoolManager objectPoolManager)
        {
            GameFrameworkGuard.NotNull(objectPoolManager, nameof(objectPoolManager));
            m_ObjectPoolManager = objectPoolManager;
            m_InstancePool = m_ObjectPoolManager.CreateMultiSpawnObjectPool<UIFormInstanceObject>("UI Instance Pool");
        }

        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="assetManager">资源管理器。</param>
        public void SetResourceManager(IAssetManager assetManager)
        {
            GameFrameworkGuard.NotNull(assetManager, nameof(assetManager));
            m_AssetManager = assetManager;
            FuiPackage = GameEntry.GetComponent<FuiPackageComponent>();
            GameFrameworkGuard.NotNull(FuiPackage, nameof(FuiPackage));
        }


        /// <summary>
        /// 设置界面辅助器。
        /// </summary>
        /// <param name="uiFormHelper">界面辅助器。</param>
        public void SetUIFormHelper(IUIFormHelper uiFormHelper)
        {
            GameFrameworkGuard.NotNull(uiFormHelper, nameof(uiFormHelper));
            m_UIFormHelper = uiFormHelper;
        }
    }
}