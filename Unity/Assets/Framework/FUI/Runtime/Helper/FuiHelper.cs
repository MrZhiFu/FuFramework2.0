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
    public static class FuiHelper
    {
        /// <summary>
        /// 实例化界面。
        /// 此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
        /// </summary>
        /// <returns>实例化后的界面。</returns>
        public static GObject InstantiateUI(string packageName, string uiName)
        {
            return UIPackage.CreateObject(packageName, uiName);
        }

        /// <summary>
        /// 创建界面。
        /// 1.将传入的UI界面实例uiInstance加上UI界面逻辑组件uiType，
        /// 2.将uiInstance作为一个子节点添加到UI界面组的显示对象下。
        /// </summary>
        /// <param name="uiInstance">界面实例。</param>
        /// <param name="uiLogicType">界面逻辑类型</param>
        /// <returns>界面。</returns>
        public static ViewBase CreateUI(object uiInstance, Type uiLogicType)
        {
            if (uiInstance is not GComponent gComponent)
            {
                Log.Error("UI界面实例不是GComponent.");
                return null;
            }

            var logicComp = gComponent.displayObject.gameObject.GetOrAddComponent(uiLogicType);
            if (logicComp is not ViewBase ui)
            {
                Log.Error("UI界面逻辑组件不是ViewBase.");
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

            if (uiGroup.displayObject == null)
            {
                Log.Error("UI界面组的显示对象信息为空.");
                return null;
            }

            if (uiGroup.displayObject.gOwner is not GComponent uiGroupComponent)
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
        public static void ReleaseUI(object uiInstance)
        {
            if (uiInstance is not GComponent component) return;
            component.Dispose();
        }
    }
}