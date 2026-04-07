using FairyGUI;
using UnityEngine;
using FuFramework.UI.Runtime;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace Hotfix.UI
{
    /// <summary>
    /// 红点UI注册器。
    /// 如果目标组件上原本就有红点组件，则直接注册到该红点组件；如果没有，则创建自动创建新的红点组件并注册。
    /// 注意：1.红点组件固定在Common包中，且名称为CompRedDot。
    ///      2.确保红点组件都来自于Common包。
    ///      3.如果需要自定义红点样式，请先拖拽Common包中的CompRedDot到界面/组件中，然后再进行自定义修改，或者使用控制器在CompRedDot上修改。
    /// </summary>
    public static class RedDotRegister
    {
        /// <summary>
        /// 注册红点到目标组件
        /// </summary>
        /// <param name="view">红点组件所属界面</param>
        /// <param name="redDotKey">红点节点Key</param>
        /// <param name="target">红点依附的目标组件</param>
        /// <param name="displayMode">红点显示模式</param>
        /// <param name="offset">红点位置偏移</param>
        public static void RegisterRedDot(ViewBase view, string redDotKey, GComponent target, CompRedDot.DisplayMode displayMode = CompRedDot.DisplayMode.DotOnly, Vector2 offset = default)
        {
            if (view is null)
            {
                FuLogger.LogError($"[RedDot] 注册红点失败 [{redDotKey}]，view 为空");
                return;
            }

            if (string.IsNullOrEmpty(redDotKey))
            {
                FuLogger.LogError($"[RedDot] 注册红点失败 [{redDotKey}]，redDotKey 为空");
                return;
            }

            if (target is null)
            {
                FuLogger.LogError($"[RedDot] 注册红点失败 [{redDotKey}]，target 为空");
                return;
            }

            // 检查 Common 包是否已加载
            var commonPkg = UIPackage.GetByName("Common");
            if (commonPkg == null)
            {
                FuLogger.LogError($"[RedDot] 注册红点失败 [{redDotKey}]，Common 包未加载，请确保已加载 Common 包");
                return;
            }

            // 检查 CompRedDot 组件是否在 Common 包中存在
            var pkgItem = commonPkg.GetItemByName("CompRedDot");
            if (pkgItem == null)
            {
                FuLogger.LogError($"[RedDot] 注册红点失败 [{redDotKey}]，Common 包中不存在 CompRedDot 组件");
                return;
            }

            // 首先检查Target是否已经有红点组件CompRedDot，如果有，则获取后调用Register方法进行注册。
            var children = target.GetChildren();
            foreach (var child in children)
            {
                if (child is not CompRedDot comp) continue;
                comp.Register(view, target, redDotKey, displayMode);
                return;
            }

            // 如果没有，则创建红点组件CompRedDot，并调用Register方法进行注册。优先使用 URL 创建，失败后使用包名+组件名创建。
            var compObj = UIPackage.CreateObjectFromURL(CompRedDot.URL) ?? UIPackage.CreateObject("Common", "CompRedDot");
            if (compObj is not CompRedDot compRedDot)
            {
                FuLogger.LogError($"[RedDot] 创建红点组件失败 [{redDotKey}]，创建的对象类型为: {compObj?.GetType().Name ?? "null"}，请检查是否正确绑定了CompRedDot组件");
                return;
            }

            target.AddChild(compRedDot);
            compRedDot.Register(view, target, redDotKey, displayMode);
            compRedDot.SetRedDotPos(offset);

            FuLogger.LogInfo($"[RedDot] 注册红点[{redDotKey}] 到 {target.name}成功");
        }
    }
}