using GameFrameX.ObjectPool;
using GameFrameX.Runtime;

namespace FuFramework.UI.Runtime
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
            /// <param name="viewBase"></param>
            /// <returns></returns>
            public static UIInstanceObject Create(string name, ViewBase viewBase)
            {
                var uiInstanceObject = ReferencePool.Acquire<UIInstanceObject>();
                uiInstanceObject.Initialize(name, viewBase);
                return uiInstanceObject;
            }

            /// <summary>
            /// 释放界面实例对象。
            /// </summary>
            /// <param name="isShutdown"></param>
            protected override void Release(bool isShutdown)
            {
                if (Target is not ViewBase viewBase)
                    throw new GameFrameworkException("[UIInstanceObject]需要释放的目标对象不是界面基类ViewBase");

                viewBase.UIView?.Dispose();
                viewBase._OnDispose();
            }
        }
    }
}