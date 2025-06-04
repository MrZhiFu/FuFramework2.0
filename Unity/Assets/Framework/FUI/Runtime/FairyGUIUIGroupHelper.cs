// GameFrameX 组织下的以及组织衍生的项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
// 
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
// 
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using FairyGUI;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FairyGUI界面组辅助器。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class FairyGUIUIGroupHelper : UIGroupHelperBase
    {
        /// <summary>
        /// 设置界面组深度。
        /// </summary>
        /// <param name="depth">界面组深度。</param>
        public override void SetDepth(int depth)
        {
            transform.localPosition = new Vector3(0, 0, depth * 100);
        }

        public override IUIGroupHelper Handler(Transform root, string groupName, string uiGroupHelperTypeName, IUIGroupHelper customUIGroupHelper)
        {
            GComponent component = new GComponent();
            GRoot.inst.AddChild(component);
            var comName = groupName;
            component.displayObject.name = comName;
            component.gameObjectName = comName;
            component.name = comName;
            component.opaque = false;
            component.AddRelation(GRoot.inst, RelationType.Width);
            component.AddRelation(GRoot.inst, RelationType.Height);
            component.MakeFullScreen();
            return GameFrameX.Runtime.Helper.CreateHelper(component.displayObject.gameObject, uiGroupHelperTypeName, (UIGroupHelperBase)customUIGroupHelper, 0);
        }
    }
}