using System;
using FairyGUI;
using GameFrameX.Asset.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FUI界面辅助器。
    /// 1.实例化界面->此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
    /// 2.创建界面->将传入的UI界面实例uiFormInstance加上UI界面逻辑组件uiFormType，并将uiFormInstance作为一个子节点添加到UI界面组的显示对象下。
    /// 3.释放界面。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class FuiFormHelper : UIFormHelperBase
    {
        /// <summary>
        /// 资源组件。
        /// </summary>
        private AssetComponent m_AssetComponent;

        private void Start()
        {
            m_AssetComponent = GameEntry.GetComponent<AssetComponent>();
            if (m_AssetComponent == null)
            {
                Log.Fatal("资源组件为空.");
            }
        }

        /// <summary>
        /// 实例化界面。
        /// 此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
        /// </summary>
        /// <param name="uiFormAsset">要实例化的界面资源。</param>
        /// <returns>实例化后的界面。</returns>
        public override object InstantiateUIForm(object uiFormAsset)
        {
            var openUIFormInfoData = (OpenUIFormInfoData)uiFormAsset;
            GameFrameworkGuard.NotNull(openUIFormInfoData, nameof(uiFormAsset));

            return UIPackage.CreateObject(openUIFormInfoData.PackageName, openUIFormInfoData.UIName);
        }

        /// <summary>
        /// 创建界面。
        /// 1.将传入的UI界面实例uiFormInstance加上UI界面逻辑组件uiFormType，
        /// 2.将uiFormInstance作为一个子节点添加到UI界面组的显示对象下。
        /// </summary>
        /// <param name="uiFormInstance">界面实例。</param>
        /// <param name="uiFormLogicType">界面逻辑类型</param>
        /// <returns>界面。</returns>
        public override IUIForm CreateUIForm(object uiFormInstance, Type uiFormLogicType)
        {
            if (uiFormInstance is not GComponent gComponent)
            {
                Log.Error("UI界面实例不是GComponent.");
                return null;
            }

            var logicComp = gComponent.displayObject.gameObject.GetOrAddComponent(uiFormLogicType);
            if (logicComp is not IUIForm uiForm)
            {
                Log.Error("UI界面逻辑组件不是IUIForm.");
                return null;
            }

            if (uiForm.IsAwake == false)
            {
                uiForm.OnAwake();
            }

            var uiGroup = uiForm.UIGroup;
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
            return uiForm;
        }

        /// <summary>
        /// 释放界面实例。
        /// </summary>
        /// <param name="uiFormInstance">要释放的界面实例。</param>
        public override void ReleaseUIForm(object uiFormInstance)
        {
            if (uiFormInstance is not GComponent component) return;
            component.Dispose();
        }
    }
}