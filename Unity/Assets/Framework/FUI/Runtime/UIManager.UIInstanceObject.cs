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
    public sealed partial class UIManager
    {
        /// <summary>
        /// 界面实例对象。
        /// 用来将界面资源对象Target和界面实例对象绑定在一起，并提供界面实例对象回收的功能。
        /// </summary>
        private sealed class UIInstanceObject : ObjectBase
        {
            /// <summary>
            /// 创建界面实例对象。
            /// </summary>
            /// <param name="name"></param>
            /// <param name="uiInstance"></param>
            /// <returns></returns>
            public static UIInstanceObject Create(string name, object uiInstance)
            {
                var uiInstanceObject = ReferencePool.Acquire<UIInstanceObject>();
                uiInstanceObject.Initialize(name, uiInstance);
                return uiInstanceObject;
            }

            /// <summary>
            /// 回收
            /// </summary>
            /// <param name="isShutdown"></param>
            protected override void Release(bool isShutdown) => FuiHelper.ReleaseUI(Target);
        }
    }
}