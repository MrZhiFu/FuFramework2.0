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
        private const int DefaultPriority = 0;

        private IUIManager m_UIManager = null;
        private EventComponent m_EventComponent = null;

        private readonly List<IUIForm> m_InternalUIFormResults = new List<IUIForm>();

        [SerializeField] private bool m_EnableOpenUIFormSuccessEvent = true;

        [SerializeField] private bool m_EnableOpenUIFormFailureEvent = true;

        // [SerializeField] private bool m_EnableOpenUIFormUpdateEvent = false;
        //
        // [SerializeField] private bool m_EnableOpenUIFormDependencyAssetEvent = false;

        [SerializeField] private bool m_EnableCloseUIFormCompleteEvent = true;

        [SerializeField] private float m_InstanceAutoReleaseInterval = 60f;

        [SerializeField] private int m_InstanceCapacity = 16;

        [SerializeField] private float m_InstanceExpireTime = 60f;

        // [SerializeField] private int m_InstancePriority = 0;

        [SerializeField] private Transform m_InstanceUGUIRoot = null;
        [SerializeField] private Transform m_InstanceFairyGUIRoot = null;

        [SerializeField] private string m_UIFormHelperTypeName = "GameFrameX.UI.FairyGUI.Runtime.FairyGUIFormHelper";

        [SerializeField] private UIFormHelperBase m_CustomUIFormHelper = null;

        [SerializeField] private string m_UIGroupHelperTypeName = "GameFrameX.UI.FairyGUI.Runtime.FairyGUIUIGroupHelper";

        [SerializeField] private UIGroupHelperBase m_CustomUIGroupHelper = null;

        [SerializeField] private UIGroup[] m_UIGroups = new UIGroup[]
        {
            new UIGroup(UIGroupConstants.Hidden.Depth, UIGroupConstants.Hidden.Name),
            new UIGroup(UIGroupConstants.Floor.Depth, UIGroupConstants.Floor.Name),
            new UIGroup(UIGroupConstants.Normal.Depth, UIGroupConstants.Normal.Name),
            new UIGroup(UIGroupConstants.Fixed.Depth, UIGroupConstants.Fixed.Name),
            new UIGroup(UIGroupConstants.Window.Depth, UIGroupConstants.Window.Name),
            new UIGroup(UIGroupConstants.Tip.Depth, UIGroupConstants.Tip.Name),
            new UIGroup(UIGroupConstants.Guide.Depth, UIGroupConstants.Guide.Name),
            new UIGroup(UIGroupConstants.BlackBoard.Depth, UIGroupConstants.BlackBoard.Name),
            new UIGroup(UIGroupConstants.Dialogue.Depth, UIGroupConstants.Dialogue.Name),
            new UIGroup(UIGroupConstants.Loading.Depth, UIGroupConstants.Loading.Name),
            new UIGroup(UIGroupConstants.Notify.Depth, UIGroupConstants.Notify.Name),
            new UIGroup(UIGroupConstants.System.Depth, UIGroupConstants.System.Name),
        };

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount
        {
            get { return m_UIManager.UIGroupCount; }
        }

        /// <summary>
        /// 获取或设置界面实例对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public float InstanceAutoReleaseInterval
        {
            get { return m_UIManager.InstanceAutoReleaseInterval; }
            set { m_UIManager.InstanceAutoReleaseInterval = m_InstanceAutoReleaseInterval = value; }
        }

        /// <summary>
        /// 获取或设置界面实例对象池的容量。
        /// </summary>
        public int InstanceCapacity
        {
            get { return m_UIManager.InstanceCapacity; }
            set { m_UIManager.InstanceCapacity = m_InstanceCapacity = value; }
        }

        /// <summary>
        /// 获取或设置界面实例对象池对象过期秒数。
        /// </summary>
        public float InstanceExpireTime
        {
            get { return m_UIManager.InstanceExpireTime; }
            set { m_UIManager.InstanceExpireTime = m_InstanceExpireTime = value; }
        }

        /*
        /// <summary>
        /// 获取或设置界面实例对象池的优先级。
        /// </summary>
        public int InstancePriority
        {
            get { return m_UIManager.InstancePriority; }
            set { m_UIManager.InstancePriority = m_InstancePriority = value; }
        }*/

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(IUIManager);
            base.Awake();
            var namespaceName = ImplementationComponentType.Namespace;

#if ENABLE_UI_FAIRYGUI
            if (!namespaceName.StartsWithFast("GameFrameX.UI.FairyGUI.Runtime"))
            {
                Debug.LogError("UI组件的 ComponentType 设置错误。请设置和 UI 系统一致的组件.");
                return;
            }

            if (m_InstanceFairyGUIRoot == null)
            {
                Debug.LogError("UI组件的 FAIRY GUI Root 设置错误。请设置");
                return;
            }

            m_InstanceFairyGUIRoot.gameObject.SetActive(true);
            if (m_InstanceUGUIRoot != null)
            {
                m_InstanceUGUIRoot.gameObject.SetActive(false);
            }

#elif ENABLE_UI_UGUI
            if (!namespaceName.StartsWithFast("GameFrameX.UI.UGUI.Runtime"))
            {
                Debug.LogError("UI组件的 ComponentType 设置错误。请设置和 UI 系统一致的组件.");
                return;
            }

            if (m_InstanceFairyGUIRoot != null)
            {
                m_InstanceFairyGUIRoot.gameObject.SetActive(false);
            }

            if (m_InstanceUGUIRoot == null)
            {
                Debug.LogError("UI组件的 UGUI Root 设置错误。请设置");
                return;
            }

            m_InstanceUGUIRoot.gameObject.SetActive(true);
#endif
            if (!m_UIFormHelperTypeName.StartsWithFast(namespaceName))
            {
                Debug.LogError("UI组件的 UI Form Helper 设置错误。请设置和 ComponentType 类型 一致.");
                return;
            }

            if (!m_UIGroupHelperTypeName.StartsWithFast(namespaceName))
            {
                Debug.LogError("UI组件的 UI Group Helper 设置错误。请设置和 ComponentType 类型 一致.");
                return;
            }

            m_UIManager = GameFrameworkEntry.GetModule<IUIManager>();
            if (m_UIManager == null)
            {
                Debug.LogError("UI manager is invalid.");
                return;
            }

            if (m_EnableOpenUIFormSuccessEvent)
            {
                m_UIManager.OpenUIFormSuccess += OnOpenUIFormSuccess;
            }

            m_UIManager.OpenUIFormFailure += OnOpenUIFormFailure;

            /*
            if (m_EnableOpenUIFormUpdateEvent)
            {
                m_UIManager.OpenUIFormUpdate += OnOpenUIFormUpdate;
            }

            if (m_EnableOpenUIFormDependencyAssetEvent)
            {
                m_UIManager.OpenUIFormDependencyAsset += OnOpenUIFormDependencyAsset;
            }*/

            if (m_EnableCloseUIFormCompleteEvent)
            {
                m_UIManager.CloseUIFormComplete += OnCloseUIFormComplete;
            }
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            m_UIManager.SetResourceManager(GameFrameworkEntry.GetModule<IAssetManager>());
            m_UIManager.SetObjectPoolManager(GameFrameworkEntry.GetModule<IObjectPoolManager>());
            m_UIManager.InstanceAutoReleaseInterval = m_InstanceAutoReleaseInterval;
            m_UIManager.InstanceCapacity = m_InstanceCapacity;
            m_UIManager.InstanceExpireTime = m_InstanceExpireTime;
            // m_UIManager.InstancePriority = m_InstancePriority;

            m_CustomUIGroupHelper = Helper.CreateHelper(m_UIGroupHelperTypeName, m_CustomUIGroupHelper);
            if (m_CustomUIGroupHelper == null)
            {
                Log.Error("Can not create UI Group helper.");
                return;
            }

            m_CustomUIGroupHelper.name = "UI Group Helper";
            Transform transform = m_CustomUIGroupHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;


            UIFormHelperBase uiFormHelper = Helper.CreateHelper(m_UIFormHelperTypeName, m_CustomUIFormHelper);
            if (uiFormHelper == null)
            {
                Log.Error("Can not create UI form helper.");
                return;
            }

            uiFormHelper.name = "UI Form Helper";
            transform = uiFormHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            m_UIManager.SetUIFormHelper(uiFormHelper);
#if ENABLE_UI_UGUI
            if (m_InstanceUGUIRoot == null)
            {
                m_InstanceUGUIRoot = new GameObject("UI Form Instances").transform;
                m_InstanceUGUIRoot.SetParent(gameObject.transform);
                m_InstanceUGUIRoot.localScale = Vector3.one;
            }

            m_InstanceUGUIRoot.gameObject.layer = LayerMask.NameToLayer("UI");
#endif
            for (int i = 0; i < m_UIGroups.Length; i++)
            {
                if (!AddUIGroup(m_UIGroups[i].Name, m_UIGroups[i].Depth))
                {
                    Log.Warning("Add UI group '{0}' failure.", m_UIGroups[i].Name);
                    continue;
                }
            }
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(int serialId)
        {
            return m_UIManager.HasUIForm(serialId);
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(string uiFormAssetName)
        {
            return m_UIManager.HasUIForm(uiFormAssetName);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(int serialId)
        {
            return m_UIManager.IsLoadingUIForm(serialId);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(string uiFormAssetName)
        {
            return m_UIManager.IsLoadingUIForm(uiFormAssetName);
        }

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUIForm(IUIForm uiForm)
        {
            return m_UIManager.IsValidUIForm(uiForm);
        }


        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        public void RefocusUIForm(UIForm uiForm)
        {
            m_UIManager.RefocusUIForm(uiForm);
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUIForm(UIForm uiForm, object userData)
        {
            m_UIManager.RefocusUIForm(uiForm, userData);
        }

        /// <summary>
        /// 设置界面是否被加锁。
        /// </summary>
        /// <param name="uiForm">要设置是否被加锁的界面。</param>
        /// <param name="locked">界面是否被加锁。</param>
        public void SetUIFormInstanceLocked(UIForm uiForm, bool locked)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            m_UIManager.SetUIFormInstanceLocked(uiForm.gameObject, locked);
        }

        private void OnOpenUIFormSuccess(object sender, OpenUIFormSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, e);
        }

        private void OnOpenUIFormFailure(object sender, OpenUIFormFailureEventArgs e)
        {
            Log.Warning($"Open UI form failure, asset name '{e.UIFormAssetName}',  pause covered UI form '{e.PauseCoveredUIForm}', error message '{e.ErrorMessage}'.");
            if (m_EnableOpenUIFormFailureEvent)
            {
                m_EventComponent.Fire(this, e);
            }
        }

        /*
        private void OnOpenUIFormUpdate(object sender, OpenUIFormUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, e);
        }

        private void OnOpenUIFormDependencyAsset(object sender, OpenUIFormDependencyAssetEventArgs e)
        {
            m_EventComponent.Fire(this, e);
        }*/

        private void OnCloseUIFormComplete(object sender, CloseUIFormCompleteEventArgs e)
        {
            m_EventComponent.Fire(this, e);
        }
    }
}