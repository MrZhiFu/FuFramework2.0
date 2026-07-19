// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 游戏框架单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T m_Instance;

        /// <summary>
        /// 获取单例对象
        /// </summary>
        public static T Instance => m_Instance ??= new T();
    }
}
