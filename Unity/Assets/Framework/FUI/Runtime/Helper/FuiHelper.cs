using System;
using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FUI界面辅助器。
    /// 1.实例化界面->此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
    /// 2.创建界面->将传入的UI界面实例uiInstance加上UI界面逻辑组件uiType，并将uiInstance作为一个子节点添加到UI界面组的显示对象下。
    /// 3.释放界面。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class FuiHelper : UIHelperBase
    {
        /// <summary>
        /// 实例化界面。
        /// 此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
        /// </summary>
        /// <param name="uiAsset">要实例化的界面资源。</param>
        /// <returns>实例化后的界面。</returns>
        public override object InstantiateUI(object uiAsset)
        {
            var openUIPackageInfo = (OpenUIPackageInfo)uiAsset;
            GameFrameworkGuard.NotNull(openUIPackageInfo, nameof(uiAsset));

            return UIPackage.CreateObject(openUIPackageInfo.PackageName, openUIPackageInfo.Name);
        }

        /// <summary>
        /// 创建界面。
        /// 1.将传入的UI界面实例uiInstance加上UI界面逻辑组件uiType，
        /// 2.将uiInstance作为一个子节点添加到UI界面组的显示对象下。
        /// </summary>
        /// <param name="uiInstance">界面实例。</param>
        /// <param name="uiLogicType">界面逻辑类型</param>
        /// <returns>界面。</returns>
        public override IUIBase CreateUI(object uiInstance, Type uiLogicType)
        {
            if (uiInstance is not GComponent gComponent)
            {
                Log.Error("UI界面实例不是GComponent.");
                return null;
            }

            var logicComp = gComponent.displayObject.gameObject.GetOrAddComponent(uiLogicType);
            if (logicComp is not IUIBase ui)
            {
                Log.Error("UI界面逻辑组件不是IUI.");
                return null;
            }

            if (ui.IsAwake == false)
            {
                ui.OnAwake();
            }

            var uiGroup = ui.UIGroup;
            if (uiGroup == null)
            {
                Log.Error("UI界面组为空.");
                return null;
            }

            var groupDisplayObjInfo = ((MonoBehaviour)uiGroup.Helper).gameObject.GetComponent<DisplayObjectInfo>();
            if (groupDisplayObjInfo == null)
            {
                Log.Error("UI界面组的显示对象信息为空.");
                return null;
            }

            if (groupDisplayObjInfo.displayObject.gOwner is not GComponent uiGroupComponent)
            {
                Log.Error("UI界面组的显示对象不是GComponent.");
                return null;
            }

            // 界面实例作为一个子节点加入到UI界面组的显示对象下
            uiGroupComponent.AddChild(gComponent);
            return ui;
        }

        /// <summary>
        /// 释放界面实例。
        /// </summary>
        /// <param name="uiInstance">要释放的界面实例。</param>
        public override void ReleaseUI(object uiInstance)
        {
            if (uiInstance is not GComponent component) return;
            component.Dispose();
        }
    }
}