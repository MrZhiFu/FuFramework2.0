using FairyGUI;
using UnityEngine;
using FuFramework.UI.Runtime;
using FuFramework.Core.Runtime;

namespace Hotfix.UI
{
    /// <summary>
    /// FUI工具类
    /// </summary>
    public class FuiUtility
    {
        /// <summary>
        /// 注册红点到指定组件
        /// </summary>
        /// <param name="view">红点组件所属界面</param>
        /// <param name="redDotKey">红点节点Key</param>
        /// <param name="target">红点依附的目标组件</param>
        /// <param name="displayMode">红点显示模式</param>
        /// <param name="offset">红点位置偏移</param>
        public static void RegisterRedDot(ViewBase view, string redDotKey, GComponent target, CompRedDot.DisplayMode displayMode = CompRedDot.DisplayMode.DotOnly, Vector2 offset = default)
        {
            if (view is null) return;
            if (string.IsNullOrEmpty(redDotKey)) return;
            if (target is null) return;

            // 首先检查Target是否已经有红点组件CompRedDot，如果有，则获取后调用CompRedDot的InitRedDot方法。
            var children = target.GetChildren();
            foreach (var child in children)
            {
                if (child is not CompRedDot comp) continue;
                comp.InitRedDot(view, target, redDotKey, displayMode);
                return;
            }

            // 如果没有，则创建红点组件CompRedDot，并调用InitRedDot方法
            if (UIPackage.CreateObject("Common", "CompRedDot") is not CompRedDot compRedDot)
            {
                FuLog.Error("创建红点组件失败!");
                return;
            }

            target.AddChild(compRedDot);
            compRedDot.InitRedDot(view, target, redDotKey, displayMode);
            compRedDot.SetRedDotPos(offset);
        }
    }
}