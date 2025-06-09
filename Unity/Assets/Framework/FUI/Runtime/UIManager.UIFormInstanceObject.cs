//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.ObjectPool;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    internal sealed partial class UIManager
    {
        /// <summary>
        /// 界面实例对象。
        /// </summary>
        private sealed class UIFormInstanceObject : ObjectBase
        {
            /// 界面资源对象
            private object m_UIFormAsset = null;

            /// 界面辅助器
            private IUIFormHelper m_UIFormHelper = null;

            public static UIFormInstanceObject Create(string name, object uiFormAsset, object uiFormInstance, IUIFormHelper uiFormHelper)
            {
                if (uiFormAsset  == null) throw new GameFrameworkException("传入的界面资源对象为空.");
                if (uiFormHelper == null) throw new GameFrameworkException("传入的界面辅助器为空.");

                UIFormInstanceObject uiFormInstanceObject = ReferencePool.Acquire<UIFormInstanceObject>();
                uiFormInstanceObject.Initialize(name, uiFormInstance);
                uiFormInstanceObject.m_UIFormAsset  = uiFormAsset;
                uiFormInstanceObject.m_UIFormHelper = uiFormHelper;
                return uiFormInstanceObject;
            }

            public override void Clear()
            {
                base.Clear();
                m_UIFormAsset  = null;
                m_UIFormHelper = null;
            }

            protected override void Release(bool isShutdown)
            {
                m_UIFormHelper.ReleaseUIForm(Target);
            }
        }
    }
}