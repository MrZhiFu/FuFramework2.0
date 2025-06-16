using UnityEngine;

namespace GameFrameX.Runtime
{
    /// <summary>
    /// 游戏框架单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GameFrameworkMonoSingleton<T> : MonoBehaviour where T : GameFrameworkMonoSingleton<T>
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
                _instance.name = "[Singleton]" + typeof(T);

                if (Application.isPlaying)
                    DontDestroyOnLoad(insObj);
                
                // 调用初始化方法
                _instance.Init();
                
                return _instance;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected virtual void Init() { }
        
        /// <summary>
        /// 销毁
        /// </summary>
        protected virtual void OnDestroy()
        {
            _instance = null;
        }
    }
}