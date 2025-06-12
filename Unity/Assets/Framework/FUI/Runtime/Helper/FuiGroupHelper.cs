using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    // /// <summary>
    // /// FairyGUI界面组辅助器。每个界面组都会被挂载一个该组件
    // /// 1.创建界面组。
    // /// 2.设置界面深度。
    // /// </summary>
    // public sealed class FuiGroupHelper: MonoBehaviour
    // {
    //     /// <summary>
    //     /// 创建界面组。
    //     /// </summary>
    //     /// <param name="root">界面组根节点对象</param>
    //     /// <param name="groupName">界面组名称。</param>
    //     /// <param name="customUIGroupHelper">界面组辅助器类型.</param>
    //     /// <returns></returns>
    //     public FuiGroupHelper CreateGroup(Transform root, string groupName, FuiGroupHelper customUIGroupHelper)
    //     {
    //         var component = new GComponent();
    //
    //         // // 设置GRoot根节点
    //         // if (GRoot.inst.displayObject.stage.gameObject.transform.parent != root)
    //         //     GRoot.inst.displayObject.stage.gameObject.transform.parent = root;
    //
    //         GRoot.inst.AddChild(component);
    //         component.displayObject.name = groupName;
    //         component.gameObjectName = groupName;
    //         component.name = groupName;
    //         component.opaque = false;
    //
    //         component.AddRelation(GRoot.inst, RelationType.Width);
    //         component.AddRelation(GRoot.inst, RelationType.Height);
    //         component.MakeFullScreen();
    //
    //         return Helper.CreateHelper(uiGroupHelperTypeName, (UIGroupHelperBase)customUIGroupHelper, 0, component.displayObject.gameObject);
    //     }
    // }
}