using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 可绑定属性，值变化时自动触发绑定事件。
    /// 功能：
    ///     1. 提供属性值的读写访问。
    ///     2. 支持注册值变化事件，当值变化时自动触发。
    ///     3. 支持注册初始值变化事件，初始化时触发一次初始值变化事件。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class BindableProperty<T>
    {
        /// <summary>
        /// 值
        /// </summary>
        private T m_Value;

        /// <summary>
        /// 值变化事件
        /// </summary>
        private Action<T> m_OnValueChanged;

        /// <summary>
        /// 值
        /// </summary>
        public T Value
        {
            get => m_Value;
            set
            {
                if (Equals(m_Value, value)) return;
                m_Value = value;
                m_OnValueChanged?.Invoke(m_Value);
            }
        }

        private BindableProperty()
        {
            m_OnValueChanged = null;
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="defaultValue">默认值</param>
        public BindableProperty(T defaultValue = default) : this()
        {
            m_Value = defaultValue;
        }

        /// <summary>
        /// 注册值变化事件
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public BindableProperty<T> Register(Action<T> callback)
        {
            callback.NotNull(nameof(callback));
            m_OnValueChanged += callback;
            return this;
        }

        /// <summary>
        /// 注册值变化事件，并触发一次初始值变化事件
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public BindableProperty<T> RegisterWithInitValue(Action<T> callback)
        {
            callback.NotNull(nameof(callback));
            callback?.Invoke(m_Value);
            return Register(callback);
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="callback">事件</param>
        public void UnRegister(Action<T> callback)
        {
            callback.NotNull(nameof(callback));
            m_OnValueChanged -= callback;
        }

        /// <summary>
        /// 清除事件
        /// </summary>
        public void Clear()
        {
            m_Value = default;
            m_OnValueChanged = null;
        }
    }
}
