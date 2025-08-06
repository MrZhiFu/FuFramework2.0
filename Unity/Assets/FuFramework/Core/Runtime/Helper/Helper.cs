using GameFrameX.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 辅助器创建器相关的实用函数。
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 创建辅助器。
        /// </summary>
        /// <typeparam name="T">要创建的辅助器类型。</typeparam>
        /// <param name="helperTypeName">要创建的辅助器类型名称。</param>
        /// <param name="customHelper">若要创建的辅助器类型为空时，使用的自定义辅助器类型。</param>
        /// <returns>创建的辅助器。</returns>
        public static T CreateHelper<T>(string helperTypeName, T customHelper) where T : MonoBehaviour
        {
            return CreateHelper(helperTypeName, customHelper, 0);
        }

        /// <summary>
        /// 创建辅助器。
        /// </summary>
        /// <typeparam name="T">要创建的辅助器类型。</typeparam>
        /// <param name="helperTypeName">要创建的辅助器类型名称。</param>
        /// <param name="customHelper">若要创建的辅助器类型为空时，使用的自定义辅助器类型。</param>
        /// <param name="index">要创建的辅助器索引。</param>
        /// <param name="target">辅助器挂载的对象。</param>
        /// <returns>创建的辅助器。</returns>
        public static T CreateHelper<T>(string helperTypeName, T customHelper, int index, GameObject target = null) where T : MonoBehaviour
        {
            // 辅助器挂载的对象为空时，创建一个新的GameObject
            if (target == null)
                target = new GameObject { name = helperTypeName };

            // 使用名称创建
            if (!string.IsNullOrEmpty(helperTypeName))
            {
                var helperType = Utility.Assembly.GetType(helperTypeName);
                if (helperType == null)
                {
                    Log.Warning("当前域中不存在类型 '{0}'.", helperTypeName);
                    return null;
                }

                if (!typeof(T).IsAssignableFrom(helperType))
                {
                    Log.Warning("类型 '{0}' 不能赋值给 '{1}'.", typeof(T).FullName, helperType.FullName);
                    return null;
                }

                return (T)target.AddComponent(helperType);
            }

            // 使用组件类型创建
            if (customHelper == null)
            {
                Log.Warning("你必须设置自定义辅助器 '{0}' 类型.", typeof(T).FullName);
                return null;
            }

            if (customHelper.gameObject.InScene())
            {
                var helper = index > 0 ? Object.Instantiate(customHelper) : customHelper;
                helper.transform.SetParent(target.transform, false);
                return helper;
            }

            return Object.Instantiate(customHelper, target.transform, false);
        }
    }
}