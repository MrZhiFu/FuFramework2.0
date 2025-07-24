namespace GameFrameX.Runtime
{
    /// <summary>
    /// 游戏框架单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GameFrameworkSingleton<T> where T : class, new()
    {
        private static T _instance;

        /// <summary>
        /// 单例对象
        /// </summary>
        public static T Instance => _instance ??= new T();
    }
}