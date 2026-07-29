using Hotfix.Framework.ReferencePools;
using System;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 界面实例对象。
    /// 目标：创建和释放界面实例对象。
    /// 功能：
    ///     1. 创建界面实例对象。
    ///     2. 释放界面实例对象。
    /// </summary>
    public sealed class ViewObject : ObjectBase
    {
        /// <summary>
        /// 创建界面实例对象。
        /// </summary>
        /// <param name="uiName"></param>
        /// <param name="viewBase"></param>
        /// <returns></returns>
        public static ViewObject Create(string uiName, WinBase viewBase)
        {
            var uiInstanceObject = ReferencePool.Acquire<ViewObject>();
            uiInstanceObject.Initialize(uiName, viewBase);
            return uiInstanceObject;
        }

        /// <summary>
        /// 释放界面实例对象。
        /// ObjectBase.OnRelease 为 protected internal abstract，ObjectBase 现与子类同属 Hotfix 程序集，
        /// 同程序集重写须保留 internal（写成 protected 会触发 CS0507），请勿改为 protected override。
        /// </summary>
        protected internal override void OnRelease()
        {
            if (Target is not WinBase viewBase)
                throw new InvalidOperationException("[UIInstanceObject] 需要释放的目标对象不是界面基类WinBase");

            try
            {
                viewBase.UIView?.Dispose();
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"[UIInstanceObject] 释放 UIView 时出现异常: {e.Message}");
            }

            viewBase._OnDispose();
        }
    }
}
