/*using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.FairyGUI.Runtime
{
    /// <summary>
    /// 管理所有顶层UI, 顶层UI都是GRoot的孩子
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/FairyGUI")]
    [RequireComponent(typeof(FairyGUIPackageComponent))]
    public sealed class FairyGUIComponent : UIComponent
    {
        private FairyGUIPackageComponent m_PackageComponent;

        private void Start()
        {
            m_PackageComponent = GetComponent<FairyGUIPackageComponent>();
            GameFrameworkGuard.NotNull(m_PackageComponent, nameof(m_PackageComponent));
        }

        /// <summary>
        /// 异步添加UI 对象
        /// </summary>
        /// <param name="creator">UI创建器</param>
        /// <param name="descFilePath">UI目录</param>
        /// <param name="layer">目标层级</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>返回创建后的UI对象</returns>
        public override async UniTask<T> AddAsync<T>(Func<object, T> creator, string descFilePath, UILayer layer, bool isFullScreen = false, object userData = null)
        {
            GameFrameworkGuard.NotNull(creator, nameof(creator));
            GameFrameworkGuard.NotNull(descFilePath, nameof(descFilePath));
            await m_PackageComponent.AddPackageAsync(descFilePath);
            T ui = creator(userData);
            Add(ui, layer);
            if (isFullScreen)
            {
                ui.MakeFullScreen();
            }

            return ui;
        }

        /// <summary>
        /// 添加UI对象
        /// </summary>
        /// <param name="creator">UI创建器</param>
        /// <param name="descFilePath">UI目录</param>
        /// <param name="layer">目标层级</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>返回创建后的UI对象</returns>
        /// <exception cref="ArgumentNullException">创建器不存在,引发参数异常</exception>
        public override T Add<T>(System.Func<object, T> creator, string descFilePath, UILayer layer, bool isFullScreen = false, object userData = null)
        {
            GameFrameworkGuard.NotNull(creator, nameof(creator));
            GameFrameworkGuard.NotNull(descFilePath, nameof(descFilePath));
            m_PackageComponent.AddPackage(descFilePath);
            T ui = creator(userData);
            Add(ui, layer);
            if (isFullScreen)
            {
                ui.MakeFullScreen();
            }

            return ui;
        }

        protected override void Awake()
        {
            IsAutoRegister = false;
            base.Awake();
        }

        /// <summary>
        /// 创建根节点
        /// </summary>
        /// <returns></returns>
        protected override UI.Runtime.UI CreateRootNode()
        {
            return new FUI(GRoot.inst);
        }

        /// <summary>
        /// 创建UI节点
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="layer">UI层级</param>
        /// <returns></returns>
        protected override UI.Runtime.UI CreateNode(object root, UILayer layer)
        {
            GComponent component = new GComponent();
            var gRoot = (FUI)root;
            gRoot.GObject.asCom.AddChild(component);
            component.z = (int)layer * 100;

            var comName = layer.ToString();

            component.displayObject.name = comName;
            component.gameObjectName = comName;
            component.name = comName;
            component.MakeFullScreen();
            component.AddRelation(gRoot.GObject.asCom, RelationType.Width);
            component.AddRelation(gRoot.GObject.asCom, RelationType.Height);
            var ui = new FUI(component);
            ui.Show();
            return ui;
        }
    }
}*/