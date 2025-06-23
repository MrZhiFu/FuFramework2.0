using System;
using UnityEngine.Scripting;

namespace GameFrameX.Runtime
{
    /// <summary>
    /// 可绑定属性，值变化时自动触发绑定事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
   
    public sealed class BindableProperty<T>
    {
        private T m_value;
        private Action<T> m_onValueChanged;

        /// <summary>
        /// 值
        /// </summary>
        public T Value
        {
            get => m_value;
            set
            {
                if (Equals(m_value, value)) return;
                m_value = value;
                m_onValueChanged?.Invoke(m_value);
            }
        }

        private BindableProperty()
        {
            m_onValueChanged = null;
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="defaultValue">默认值</param>
       
        public BindableProperty(T defaultValue = default) : this()
        {
            m_value = defaultValue;
        }


        /// <summary>
        /// 注册值变化事件
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
       
        public BindableProperty<T> Register(Action<T> callback)
        {
            GameFrameworkGuard.NotNull(callback, nameof(callback));
            m_onValueChanged += callback;
            return this;
        }

        /// <summary>
        /// 注册值变化事件，并触发一次初始值变化事件
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
       
        public BindableProperty<T> RegisterWithInitValue(Action<T> callback)
        {
            GameFrameworkGuard.NotNull(callback, nameof(callback));
            callback?.Invoke(m_value);
            return Register(callback);
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="callback">事件</param>
       
        public void UnRegister(Action<T> callback)
        {
            GameFrameworkGuard.NotNull(callback, nameof(callback));
            m_onValueChanged -= callback;
        }

        /// <summary>
        /// 清除事件
        /// </summary>
       
        public void Clear()
        {
            m_onValueChanged = null;
        }
    }
}