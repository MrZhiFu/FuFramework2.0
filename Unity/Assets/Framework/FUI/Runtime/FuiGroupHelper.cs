using FairyGUI;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FairyGUI界面组辅助器。
    /// 1.设置界面深度。
    /// 2.创建界面组。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class FuiGroupHelper : UIGroupHelperBase
    {
        /// <summary>
        /// 设置界面组深度。
        /// </summary>
        /// <param name="depth">界面组深度。</param>
        public override void SetDepth(int depth)
        {
            transform.localPosition = new Vector3(0, 0, depth * 100);
        }

        /// <summary>
        /// 创建界面组。
        /// </summary>
        /// <param name="root">界面组根节点对象</param>
        /// <param name="groupName">界面组名称。</param>
        /// <param name="uiGroupHelperTypeName">界面组辅助器类型名。</param>
        /// <param name="customUIGroupHelper">界面组辅助器类型.</param>
        /// <returns></returns>
        public override IUIGroupHelper CreateGroup(Transform root, string groupName, string uiGroupHelperTypeName, IUIGroupHelper customUIGroupHelper)
        {
            var component = new GComponent();

            // 设置GRoot根节点
            if (GRoot.inst.displayObject.stage.gameObject.transform.parent != root)
                GRoot.inst.displayObject.stage.gameObject.transform.parent = root;

            GRoot.inst.AddChild(component);
            component.displayObject.name = groupName;
            component.gameObjectName = groupName;
            component.name = groupName;
            component.opaque = false;

            component.AddRelation(GRoot.inst, RelationType.Width);
            component.AddRelation(GRoot.inst, RelationType.Height);
            component.MakeFullScreen();

            return GameFrameX.Runtime.Helper.CreateHelper(uiGroupHelperTypeName, (UIGroupHelperBase)customUIGroupHelper, 0,
                component.displayObject.gameObject);
        }
    }
}