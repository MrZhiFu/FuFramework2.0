using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// Object相关的扩展方法。
    /// 功能：
    ///     1. 检查对象是否为null。
    ///     2. 检查对象是否不为null。
    ///     3. 检查对象是否为null,当为null时抛出异常。
    /// </summary>
    public static class ObjectEx
    {
        /// <summary>
        /// 检查对象是否为null。
        /// 对已销毁的 UnityEngine.Object（“假 null”）同样返回 true：
        /// 以 object 静态类型比较时 == 走引用相等，识别不出 Unity 重载的 ==。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsNull(this object self)
        {
            if (ReferenceEquals(self, null)) return true;

            // UnityEngine.Object 重载了 ==：对象被 Destroy 后引用非 null，但与 null 比较为 true
            if (self is UnityEngine.Object unityObject) return unityObject == null;

            return false;
        }

        /// <summary>
        /// 检查对象是否不为null
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsNotNull(this object self) => !self.IsNull();

        /// <summary>
        /// 检查对象是否为null,当为null时抛出异常
        /// </summary>
        /// <param name="self">对象值</param>
        /// <param name="name">异常信息</param>
        /// <exception cref="ArgumentNullException">参数为空的异常</exception>
        public static void CheckNull(this object self, string name)
        {
            if (self.IsNull()) throw new ArgumentNullException(name, " 不能为空.");
        }
    }
}
