using UnityEngine;

// ReSharper disable once CheckNamespace
// ReSharper disable StaticMemberInGenericType
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架Mono单例(线程安全)
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T m_Instance; // 单例对象
        
        private static bool m_IsInitialized;         // 是否已初始化--防止重复初始化
        private static bool m_IsApplicationQuitting; // 是否应用退出中--防止应用退出时访问已销毁的单例

        private static readonly object m_Lock = new(); // 锁对象--防止多线程竞争

        /// <summary>
        /// 单例对象
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_IsApplicationQuitting)
                {
                    Log.Warning($"[MonoSingleton] 应用程序退出，{typeof(T)} 已被销毁，返回null");
                    return null;
                }

                lock (m_Lock)
                {
                    if (m_Instance != null) return m_Instance;
                    
                    m_Instance = FindFirstObjectByType<T>();
                    if (m_Instance != null)
                    {
                        // 确保手动放置在场景中的实例也被正确初始化
                        if (!m_IsInitialized) m_Instance.InitializeSingleton();
                        return m_Instance;
                    }

                    // 创建新实例
                    var singletonObject = new GameObject();
                    m_Instance = singletonObject.AddComponent<T>();
                    singletonObject.name = $"[Singleton] {typeof(T).Name}";

                    DontDestroyOnLoad(singletonObject);
                    m_Instance.InitializeSingleton();

                    return m_Instance;
                }
            }
        }

        /// <summary>
        /// Awake生命周期：处理场景中手动放置的单例组件
        /// </summary>
        private void Awake()
        {
            // 编辑器模式下跳过
            if (!Application.isPlaying) return;

            lock (m_Lock)
            {
                // 防止在场景中手动放置了多个单例组件而导致创建重复实例
                if (m_Instance != null && m_Instance != this)
                {
                    Log.Warning($"[MonoSingleton] 场景中已存在同类型的单例组件 '{typeof(T)}', 该单例{gameObject.name}被立即销毁!");
                    DestroyImmediate(gameObject);
                    return;
                }

                // 确保场景中手动放置的单例组件也被正确初始化
                if (m_Instance == null)
                {
                    m_Instance = this as T;
                    DontDestroyOnLoad(gameObject);

                    if (!m_IsInitialized) 
                        InitializeSingleton();
                }
            }
        }

        /// <summary>
        /// 应用程序退出
        /// </summary>
        private void OnApplicationQuit() => m_IsApplicationQuitting = true;

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            lock (m_Lock)
            {
                if (m_Instance != this) return;
                Dispose();
                m_Instance = null;
                m_IsInitialized = false;
            }
        }

        /// <summary>
        /// 初始化单例（确保只初始化一次）
        /// </summary>
        private void InitializeSingleton()
        {
            if (m_IsInitialized) return;
            m_IsInitialized = true;
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected virtual void Init() { }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose() { }
    }
}