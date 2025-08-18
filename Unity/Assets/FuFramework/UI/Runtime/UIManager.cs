using System;
using FairyGUI;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    public sealed partial class UIManager : MonoSingleton<UIManager>
    {
        private Dictionary<int, string> m_LoadingDict;       // 正在加载中的界面字典, key为界面Id, value为界面名称
        private Queue<ViewBase>         m_WaitRecycleQueue;  // 关闭后待回收的界面集合

        private ObjectPoolComponent m_ObjectPoolManager; // 对象池管理器
        private EventComponent      m_EventComponent;    // 事件组件

        private IObjectPool<UIInstanceObject> m_InstancePool; // 界面实例对象池

        private int  m_SerialId;   // 界面序列号，每打开一个界面就加1
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
            m_UIGroupDict       = new Dictionary<UILayer, UIGroup>();
            m_LoadingDict       = new Dictionary<int, string>();
            m_WaitRecycleQueue  = new Queue<ViewBase>();

            m_ObjectPoolManager = GameEntry.GetComponent<ObjectPoolComponent>();
            m_InstancePool      = m_ObjectPoolManager.CreateObjectPool<UIInstanceObject>("UIInstanceObjectPool");

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            GameEntry.GetComponent<AssetComponent>();

            m_SerialId   = 0;
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
                Log.Error("[UIManager]添加UI组 '{0}' 失败 .", layer.ToString());
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
        protected override void Dispose()
        {
            Shutdown();
        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        private void Shutdown()
        {
            m_IsShutdown = true;
            m_UIGroupDict.Clear();
            m_LoadingDict.Clear();
            m_WaitRecycleQueue.Clear();
        }
    }
}