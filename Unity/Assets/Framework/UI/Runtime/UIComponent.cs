//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;
using GameFrameX.Asset.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.ObjectPool;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/UI")]
    [UnityEngine.Scripting.Preserve]
    public partial class UIComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 界面管理器
        /// </summary>
        private IUIManager m_UIManager = null;

        /// <summary>
        /// 事件组件
        /// </summary>
        private EventComponent m_EventComponent = null;

        /// <summary>
        /// 内部的界面实例列表。缓存作用，避免频繁创建查找结果列表。
        /// 1.当获取所有已加载的界面时，此列表就是所有已加载的界面实例。
        /// 2.当获取界面名称对应的界面，此列表是名称对应的界面
        /// </summary>
        private readonly List<IUIForm> m_InternalUIFormResults = new();


        [Header("是否激活打开界面成功事件")]
        [SerializeField] private bool m_EnableOpenUIFormSuccessEvent = true;

        [Header("是否激活打开界面失败事件")]
        [SerializeField] private bool m_EnableOpenUIFormFailureEvent = true;

        [Header("是否激活打开界面成功事件")]
        [SerializeField] private bool m_EnableCloseUIFormCompleteEvent = true;

        [Header("界面实例对象池自动释放可释放对象的间隔秒数")]
        [SerializeField] private float m_InstanceAutoReleaseInterval = 60f;

        [Header("界面实例对象池的容量")]
        [SerializeField] private int m_InstanceCapacity = 16;

        [Header("界面实例对象池对象过期秒数")]
        [SerializeField] private float m_InstanceExpireTime = 60f;

        [Header("界面辅助器类名")]
        [SerializeField] private string m_UIFormHelperTypeName = "GameFrameX.UI.FairyGUI.Runtime.FairyGUIFormHelper";

        [Header("界面辅助器")]
        [SerializeField] private UIFormHelperBase m_CustomUIFormHelper = null;

        [Header("界面组辅助器类名")]
        [SerializeField] private string m_UIGroupHelperTypeName = "GameFrameX.UI.FairyGUI.Runtime.FairyGUIUIGroupHelper";

        [Header("界面组辅助器")]
        [SerializeField] private UIGroupHelperBase m_CustomUIGroupHelper = null;

        [Header("界面组定义")]
        [SerializeField] private UIGroup[] m_UIGroups =
        {
            //@formatter:off
            new(UIGroupConstants.Hidden.Depth,     UIGroupConstants.Hidden.Name),
            new(UIGroupConstants.Floor.Depth,      UIGroupConstants.Floor.Name),
            new(UIGroupConstants.Normal.Depth,     UIGroupConstants.Normal.Name),
            new(UIGroupConstants.Fixed.Depth,      UIGroupConstants.Fixed.Name),
            new(UIGroupConstants.Window.Depth,     UIGroupConstants.Window.Name),
            new(UIGroupConstants.Tip.Depth,        UIGroupConstants.Tip.Name),
            new(UIGroupConstants.Guide.Depth,      UIGroupConstants.Guide.Name),
            new(UIGroupConstants.BlackBoard.Depth, UIGroupConstants.BlackBoard.Name),
            new(UIGroupConstants.Dialogue.Depth,   UIGroupConstants.Dialogue.Name),
            new(UIGroupConstants.Loading.Depth,    UIGroupConstants.Loading.Name),
            new(UIGroupConstants.Notify.Depth,     UIGroupConstants.Notify.Name),
            new(UIGroupConstants.System.Depth,     UIGroupConstants.System.Name),
            //@formatter:on
        };

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount => m_UIManager.UIGroupCount;

        /// <summary>
        /// 获取或设置界面实例对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public float InstanceAutoReleaseInterval
        {
            get => m_UIManager.InstanceAutoReleaseInterval;
            set => m_UIManager.InstanceAutoReleaseInterval = m_InstanceAutoReleaseInterval = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池的容量。
        /// </summary>
        public int InstanceCapacity
        {
            get => m_UIManager.InstanceCapacity;
            set => m_UIManager.InstanceCapacity = m_InstanceCapacity = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池对象过期秒数。
        /// </summary>
        public float InstanceExpireTime
        {
            get => m_UIManager.InstanceExpireTime;
            set => m_UIManager.InstanceExpireTime = m_InstanceExpireTime = value;
        }

        
        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(IUIManager);

            base.Awake();

            m_UIManager = GameFrameworkEntry.GetModule<IUIManager>();
            if (m_UIManager == null)
            {
                Debug.LogError("UI manager is invalid.");
                return;
            }

            m_UIManager.OpenUIFormSuccess += OnOpenUIFormSuccess;
            m_UIManager.OpenUIFormFailure += OnOpenUIFormFailure;

            if (m_EnableCloseUIFormCompleteEvent)
            {
                m_UIManager.CloseUIFormComplete += OnCloseUIFormComplete;
            }
        }

        private void Start()
        {
            var baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base组件为空.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event组件为空.");
                return;
            }

            m_UIManager.SetResourceManager(GameFrameworkEntry.GetModule<IAssetManager>());
            m_UIManager.SetObjectPoolManager(GameFrameworkEntry.GetModule<IObjectPoolManager>());
            m_UIManager.InstanceAutoReleaseInterval = m_InstanceAutoReleaseInterval;
            m_UIManager.InstanceCapacity            = m_InstanceCapacity;
            m_UIManager.InstanceExpireTime          = m_InstanceExpireTime;

            // 创建UI组辅助器
            m_CustomUIGroupHelper = Helper.CreateHelper(m_UIGroupHelperTypeName, m_CustomUIGroupHelper);
            if (m_CustomUIGroupHelper == null)
            {
                Log.Error("找不到UI组辅助器类.");
                return;
            }

            m_CustomUIGroupHelper.name = "UI Group Helper";
            var groupTrs = m_CustomUIGroupHelper.transform;
            groupTrs.SetParent(transform);
            groupTrs.localScale = Vector3.one;

            // 创建UI界面辅助器，并设置到UI管理器中
            var uiFormHelper = Helper.CreateHelper(m_UIFormHelperTypeName, m_CustomUIFormHelper);
            if (uiFormHelper == null)
            {
                Log.Error("找不到UI界面辅助器类.");
                return;
            }

            uiFormHelper.name = "UI Form Helper";
            groupTrs          = uiFormHelper.transform;
            groupTrs.SetParent(transform);
            groupTrs.localScale = Vector3.one;
            m_UIManager.SetUIFormHelper(uiFormHelper);

            // 遍历所有UI组，并添加UI组
            foreach (var group in m_UIGroups)
            {
                if (AddUIGroup(group.Name, group.Depth)) continue;
                Log.Warning("添加UI组 '{0}' 失败 .", group.Name);
            }
        }
        

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(int serialId) => m_UIManager.HasUI(serialId);

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(string uiFormAssetName) => m_UIManager.HasUI(uiFormAssetName);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(int serialId) => m_UIManager.IsLoadingUI(serialId);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(string uiFormAssetName) => m_UIManager.IsLoadingUI(uiFormAssetName);

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUIForm(IUIForm uiForm) => m_UIManager.IsValidUI(uiForm);

        /// <summary>
        /// 设置界面是否被加锁。
        /// </summary>
        /// <param name="uiForm">要设置是否被加锁的界面。</param>
        /// <param name="locked">界面是否被加锁。</param>
        public void SetUIFormInstanceLocked(UIForm uiForm, bool locked)
        {
            if (uiForm == null)
            {
                Log.Warning("UI界面为空.");
                return;
            }

            m_UIManager.SetUIInstanceLocked(uiForm.gameObject, locked);
        }
        

        /// <summary>
        /// 界面打开成功事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOpenUIFormSuccess(object sender, OpenUISuccessEventArgs e)
        {
            if (m_EnableOpenUIFormSuccessEvent)
                m_EventComponent.Fire(this, e);
        }

        /// <summary>
        /// 界面打开失败事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOpenUIFormFailure(object sender, OpenUIFailureEventArgs e)
        {
            Log.Warning($"Open UI form failure, asset name '{e.UIFormAssetName}',  pause covered UI form '{e.PauseCoveredUIForm}', error message '{e.ErrorMessage}'.");
            if (m_EnableOpenUIFormFailureEvent)
                m_EventComponent.Fire(this, e);
        }

        /// <summary>
        /// 界面关闭完成事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCloseUIFormComplete(object sender, CloseUICompleteEventArgs e)
        {
            if (m_EnableCloseUIFormCompleteEvent)
                m_EventComponent.Fire(this, e);
        }
    }
}