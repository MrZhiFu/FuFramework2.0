using System;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ObjectPool;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 界面实例对象。
    /// 目标：创建和销毁界面实例对象。
    /// 功能：
    ///     1. 创建界面实例对象。
    ///     2. 销毁界面实例对象。
    /// </summary>
    public sealed class WinObject : ObjectBase
    {
        /// <summary>
        /// 创建界面实例对象。
        /// </summary>
        /// <param name="winName"></param>
        /// <param name="winBase"></param>
        /// <returns></returns>
        public static WinObject Create(string winName, WinBase winBase)
        {
            var winObject = GlobalModule.ReferencePoolModule.Acquire<WinObject>();
            winObject.Initialize(winName, winBase);
            return winObject;
        }

        /// <summary>
        /// 销毁界面实例对象
        /// </summary>
        protected internal override void OnDispose()
        {
            if (Target is not WinBase winBase)
                throw new InvalidOperationException("[UIInstanceObject] 需要销毁的目标对象不是界面基类WinBase");

            try
            {
                winBase.WinUI?.Dispose();
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"[UIInstanceObject] 销毁 WinUI 时出现异常: {e.Message}");
            }

            winBase._OnDispose();
        }
    }
}