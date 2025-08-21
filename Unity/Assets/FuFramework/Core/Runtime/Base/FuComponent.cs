using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架组件抽象基类。
    /// 实现了组件的自动注册功能。
    /// </summary>
    public abstract class FuComponent : MonoBehaviour
    {
        /// <summary>
        /// 是否自动注册
        /// </summary>
        protected bool IsAutoRegister { get; set; } = true;

        /// <summary>
        /// 接口类的类型
        /// </summary>
        protected Type InterfaceComponentType = null;

        /// <summary>
        /// 实现类的类型
        /// </summary>
        protected Type ImplComponentType = null;

        /// <summary>
        /// 游戏框架组件类型。
        /// </summary>
        [SerializeField] protected string componentType = string.Empty;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected virtual void Awake()
        {
            GameEntry.RegisterComponent(this);
            
            if (!IsAutoRegister) return;
            
            FuGuard.NotNull(ImplComponentType,      nameof(ImplComponentType));
            FuGuard.NotNull(InterfaceComponentType, nameof(InterfaceComponentType));
            
            FuEntry.RegisterModule(InterfaceComponentType, ImplComponentType);
        }
    }
}