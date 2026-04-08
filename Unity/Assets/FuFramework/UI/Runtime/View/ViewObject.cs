using FuFramework.Core.Runtime;
using FuFramework.ObjectPool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面实例对象。
    /// 职责：创建和释放界面实例对象。
    /// 核心功能:
    /// 1. 创建界面实例对象。
    /// 2. 释放界面实例对象。
    /// </summary>
    public sealed class ViewObject : ObjectBase
    {
        /// <summary>
        /// 创建界面实例对象。
        /// </summary>
        /// <param name="uiName"></param>
        /// <param name="viewBase"></param>
        /// <returns></returns>
        public static ViewObject Create(string uiName, ViewBase viewBase)
        {
            var uiInstanceObject = ReferencePool.Runtime.ReferencePool.Acquire<ViewObject>();
            uiInstanceObject.Initialize(uiName, viewBase);
            return uiInstanceObject;
        }

        /// <summary>
        /// 释放界面实例对象。
        /// </summary>
        /// <param name="isShutdown"></param>
        protected override void Release(bool isShutdown)
        {
            if (Target is not ViewBase viewBase)
                throw new FuException("[UIInstanceObject] 需要释放的目标对象不是界面基类ViewBase");

            viewBase.UIView?.Dispose();
            viewBase._OnDispose();
        }
    }
}