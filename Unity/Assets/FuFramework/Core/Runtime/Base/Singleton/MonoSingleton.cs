using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架Mono单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        /// <summary>
        /// 单例对象
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = (T)FindObjectOfType(typeof(T));
                if (_instance != null) return _instance;

                var insObj = new GameObject();
                _instance      = insObj.AddComponent<T>();
                _instance.name = "[Singleton]" + typeof(T).Name;

                if (Application.isPlaying)
                    DontDestroyOnLoad(insObj);

                // 调用初始化方法
                _instance.Init();

                return _instance;
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
            _instance = null;
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