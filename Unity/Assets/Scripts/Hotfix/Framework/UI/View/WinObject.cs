using Hotfix.Framework.ReferencePools;
using System;
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
            var winObject = ReferencePool.Acquire<WinObject>();
            winObject.Initialize(winName, winBase);
            return winObject;
        }

        /// <summary>
        /// 释放界面实例对象
        /// </summary>
        protected internal override void OnRelease()
        {
            if (Target is not WinBase winBase)
                throw new InvalidOperationException("[UIInstanceObject] 需要释放的目标对象不是界面基类WinBase");

            try
            {
                winBase.WinUI?.Dispose();
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"[UIInstanceObject] 释放 WinUI 时出现异常: {e.Message}");
            }

            winBase._OnDispose();
        }
    }
}