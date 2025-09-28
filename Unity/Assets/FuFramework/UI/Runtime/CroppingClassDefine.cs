// ===================================================
// 防裁剪代码 - 自动生成
// 程序集: FuFramework.UI.Runtime
// 生成时间: 2025-09-26 18:48:12
// 保存位置: Assets/FuFramework/UI/Runtime
// ===================================================

using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 防裁剪类定义
    /// 防止 IL2CPP 代码裁剪时移除重要类型
    /// 自动生成于目标类型所在文件夹
    /// </summary>
    public static class CroppingClassDefine
    {
        /// <summary>
        /// 防止代码裁剪的方法
        /// 在场景加载前执行，确保所有需要的类型都被保留
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PreventCodeStripping()
        {
            _ = typeof(CloseUICompleteEventArgs);
            _ = typeof(CroppingClassDefine);
            _ = typeof(CustomLoader);
            _ = typeof(FuiEventRegister);
            _ = typeof(FuiPackageManager);
            _ = typeof(FuiPathFinderHelper);
            _ = typeof(GObjectExtensions);
            _ = typeof(ICustomComp);
            _ = typeof(LRUCache);
            _ = typeof(OpenUIFailureEventArgs);
            _ = typeof(OpenUISuccessEventArgs);
            _ = typeof(UIGroup);
            _ = typeof(UIInfo);
            _ = typeof(UILayer);
            _ = typeof(UIManager);
            _ = typeof(UITweenType);
            _ = typeof(UIVisibleChangedEventArgs);
            _ = typeof(ViewBase);
        }
    }
}
