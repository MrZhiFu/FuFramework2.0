using UnityEngine;

// ReSharper disable once CheckNamespace
// ReSharper disable StaticMemberInGenericType
using AOT.Framework.Core.Log;
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 游戏框架Mono单例(线程安全)
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        /// <summary>
        /// 单例对象
        /// </summary>
        private static T m_Instance;

        /// <summary>
        /// 是否已初始化--防止重复初始化
        /// </summary>
        private static bool m_IsInitialized;

        /// <summary>
        /// 单例对象
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_Instance != null)
                {
                    return m_Instance;
                }

                m_Instance = FindFirstObjectByType<T>();
                if (m_Instance != null)
                {
                    // 确保手动放置在场景中的实例也被正确初始化
                    if (!m_IsInitialized)
                    {
                        m_Instance.Init();
                    }
                    return m_Instance;
                }

                // 创建新实例
                var singletonObject = new GameObject();
                m_Instance           = singletonObject.AddComponent<T>();
                singletonObject.name = $"[Singleton] {typeof(T).Name}";

                DontDestroyOnLoad(singletonObject);
                m_Instance.Init();

                return m_Instance;
            }
        }

        /// <summary>
        /// Awake生命周期：处理场景中手动放置的单例组件
        /// </summary>
        private void Awake()
        {
            // 编辑器模式下跳过
            if (!Application.isPlaying) return;

            // 防止在场景中手动放置了多个单例组件而导致创建重复实例
            if (m_Instance && m_Instance != this)
            {
                FuLogger.LogWarning($"[MonoSingleton] 场景中已存在同类型的单例组件 '{typeof(T)}', 该单例{gameObject.name}被立即销毁!");
                DestroyImmediate(gameObject);
                return;
            }

            // 确保场景中手动放置的单例组件也被正确初始化
            if (!m_Instance)
            {
                m_Instance = this as T;
                DontDestroyOnLoad(gameObject);

                if (!m_IsInitialized)
                    Init();
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            if (m_Instance != this) return;
            OnDispose();
            m_Instance      = null;
            m_IsInitialized = false;
        }

        /// <summary>
        /// 初始化单例（确保只初始化一次）
        /// </summary>
        private void Init()
        {
            if (m_IsInitialized) return;
            m_IsInitialized = true;
            OnInit();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void OnDispose() { }
    }
}
