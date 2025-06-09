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
        /// 用来将界面资源对象Target和界面实例对象绑定在一起，并提供界面实例对象回收的功能。
        /// </summary>
        private sealed class UIInstanceObject : ObjectBase
        {
            /// 界面辅助器
            private IUIFormHelper m_UIFormHelper = null;

            /// <summary>
            /// 创建界面实例对象。
            /// </summary>
            /// <param name="name"></param>
            /// <param name="uiFormInstance"></param>
            /// <param name="uiFormHelper"></param>
            /// <returns></returns>
            /// <exception cref="GameFrameworkException"></exception>
            public static UIInstanceObject Create(string name, object uiFormInstance, IUIFormHelper uiFormHelper)
            {
                if (uiFormHelper == null) throw new GameFrameworkException("传入的界面辅助器为空.");

                var uiFormInstanceObject = ReferencePool.Acquire<UIInstanceObject>();
                uiFormInstanceObject.Initialize(name, uiFormInstance);
                uiFormInstanceObject.m_UIFormHelper = uiFormHelper;
                return uiFormInstanceObject;
            }

            /// <summary>
            /// 清理
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                m_UIFormHelper = null;
            }

            /// <summary>
            /// 回收
            /// </summary>
            /// <param name="isShutdown"></param>
            protected override void Release(bool isShutdown)
            {
                m_UIFormHelper.ReleaseUI(Target);
            }
        }
    }
}