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
using GameFrameX.Event.Runtime;
using GameFrameX.ObjectPool;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    public sealed partial class UIManager : GameFrameworkMonoSingleton<UIManager>
    {
        private Dictionary<int, string> m_LoadingDict;      // 正在加载的界面集合, key为界面Id, value为界面名称
        private HashSet<int>            m_WaitReleaseSet;   // 待释放的界面集合，int为界面Id
        private Queue<ViewBase>         m_WaitRecycleQueue; // 待回收的界面集合

        private AssetComponent      m_AssetManager;          // 资源管理器
        private ObjectPoolComponent m_ObjectPoolManager;     // 对象池管理器
        private EventComponent      m_EventComponent = null; // 事件组件

        private IObjectPool<UIInstanceObject> m_InstancePool; // 界面实例对象池

        private int  m_Serial;     // 界面序列号，每打开一个界面就加1
        private bool m_IsShutdown; // 是否是关机

        [Header("界面实例对象池自动释放可释放对象的间隔秒数")]
        [SerializeField] private float m_InstanceAutoReleaseInterval = 60f;

        [Header("界面实例对象池的容量")]
        [SerializeField] private int m_InstanceCapacity = 16;

        [Header("界面实例对象池对象过期秒数")]
        [SerializeField] private float m_InstanceExpireTime = 60f;


        /// <summary>
        /// 初始化界面管理器的新实例。
        /// </summary>
        protected override void Init()
        {
            m_UIGroupDict      = new Dictionary<UILayer, UIGroup>();
            m_LoadingDict      = new Dictionary<int, string>();
            m_WaitReleaseSet   = new HashSet<int>();
            m_WaitRecycleQueue = new Queue<ViewBase>();

            m_ObjectPoolManager = GameEntry.GetComponent<ObjectPoolComponent>();
            m_InstancePool      = m_ObjectPoolManager.CreateMultiSpawnObjectPool<UIInstanceObject>("UIInstanceObjectPool");

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            m_AssetManager   = GameEntry.GetComponent<AssetComponent>();

            m_Serial     = 0;
            m_IsShutdown = false;

            InstanceAutoReleaseInterval = m_InstanceAutoReleaseInterval;
            InstanceCapacity            = m_InstanceCapacity;
            InstanceExpireTime          = m_InstanceExpireTime;

            // 设置GRoot根节点
            GRoot.inst.displayObject.stage.gameObject.transform.parent = transform;

            // 遍历所有UI层级，并添加UI组
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                if (AddUIGroup(layer)) continue;
                Log.Warning("添加UI组 '{0}' 失败 .", layer.ToString());
            }
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

        /// <summary>
        /// 界面管理器轮询。
        /// </summary>
        protected void Update()
        {
            while (m_WaitRecycleQueue.Count > 0)
            {
                var ui = m_WaitRecycleQueue.Dequeue();
                RecycleUI(ui);
            }

            foreach (var (_, group) in m_UIGroupDict)
            {
                group.OnUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }


        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Shutdown();
        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        private void Shutdown()
        {
            m_IsShutdown = true;
            CloseAllLoadedUIs();
            m_UIGroupDict.Clear();
            m_LoadingDict.Clear();
            m_WaitReleaseSet.Clear();
            m_WaitRecycleQueue.Clear();
        }
    }
}