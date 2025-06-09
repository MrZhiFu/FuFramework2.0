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
            private IUIHelper _iuiHelper = null;

            /// <summary>
            /// 创建界面实例对象。
            /// </summary>
            /// <param name="name"></param>
            /// <param name="uiInstance"></param>
            /// <param name="iuiHelper"></param>
            /// <returns></returns>
            /// <exception cref="GameFrameworkException"></exception>
            public static UIInstanceObject Create(string name, object uiInstance, IUIHelper iuiHelper)
            {
                if (iuiHelper == null) throw new GameFrameworkException("传入的界面辅助器为空.");

                var uiInstanceObject = ReferencePool.Acquire<UIInstanceObject>();
                uiInstanceObject.Initialize(name, uiInstance);
                uiInstanceObject._iuiHelper = iuiHelper;
                return uiInstanceObject;
            }

            /// <summary>
            /// 清理
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                _iuiHelper = null;
            }

            /// <summary>
            /// 回收
            /// </summary>
            /// <param name="isShutdown"></param>
            protected override void Release(bool isShutdown)
            {
                _iuiHelper.ReleaseUI(Target);
            }
        }
    }
}