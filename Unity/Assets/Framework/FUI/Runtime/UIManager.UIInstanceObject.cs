//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FairyGUI;
using GameFrameX.ObjectPool;
using GameFrameX.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    public sealed partial class UIManager
    {
        /// <summary>
        /// 界面实例对象。
        /// 用来将界面FUI对象和界面实例对象绑定在一起，并提供界面实例对象回收的功能。
        /// </summary>
        private sealed class UIInstanceObject : ObjectBase
        {
            /// <summary>
            /// 创建界面实例对象。
            /// </summary>
            /// <param name="name"></param>
            /// <param name="uiView"></param>
            /// <returns></returns>
            public static UIInstanceObject Create(string name, GComponent uiView)
            {
                var uiInstanceObject = ReferencePool.Acquire<UIInstanceObject>();
                uiInstanceObject.Initialize(name, uiView);
                return uiInstanceObject;
            }

            /// <summary>
            /// 释放界面实例对象。
            /// </summary>
            /// <param name="isShutdown"></param>
            protected override void Release(bool isShutdown) => FuiHelper.ReleaseUI(Target);
        }
    }
}