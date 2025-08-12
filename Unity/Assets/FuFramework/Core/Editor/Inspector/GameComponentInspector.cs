using System.Collections.Generic;
using FuFramework.Core.Runtime;
using UnityEditor;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 游戏中各个组件的Inspector属性面板抽象基类。
    /// 继承该类，实现自定义的Inspector，并在Enable方法中初始化组件类型名称数组。
    /// </summary>
    public abstract class GameComponentInspector : FuFrameworkInspector
    {
        /// 组件类型
        protected SerializedProperty ComponentType;

        /// 组件类型名称数组
        protected string[] ComponentTypeNames;

        /// 组件类型名称索引
        protected int ComponentTypeNameIndex;

        
        /// <summary>
        /// 初始化Inspector
        /// </summary>
        private void OnEnable()
        {
            ComponentType = serializedObject.FindProperty("componentType");
            Enable();
            RefreshTypeNames();
        }

        /// <summary>
        /// 绘制Inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                var componentTypeSelectedIndex = EditorGUILayout.Popup("ComponentType", ComponentTypeNameIndex, ComponentTypeNames);
                if (componentTypeSelectedIndex != ComponentTypeNameIndex)
                {
                    ComponentTypeNameIndex    = componentTypeSelectedIndex;
                    ComponentType.stringValue = componentTypeSelectedIndex <= 0 ? null : ComponentTypeNames[componentTypeSelectedIndex];
                }
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        /// <summary>
        /// 编译完成后刷新组件类型名称数组
        /// </summary>
        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        /// <summary>
        /// 组件Inspector启用时调用
        /// </summary>
        protected virtual void Enable() { }

        /// <summary>
        /// 刷新组件类型名称数组
        /// </summary>
        protected abstract void RefreshTypeNames();

        /// <summary>
        /// 刷新组件类型名称数组。
        /// 传入类型，获取该类型下的所有子类名称，并添加到数组中
        /// </summary>
        /// <param name="type"></param>
        protected void RefreshComponentTypeNames(System.Type type)
        {
            var managerTypeNames = new List<string> { NoneOptionName };
            managerTypeNames.AddRange(Utility.Assembly.GetRuntimeTypeNames(type));

            ComponentTypeNames     = managerTypeNames.ToArray();
            ComponentTypeNameIndex = 0;

            if (!ComponentType.stringValue.IsNullOrEmpty())
            {
                ComponentTypeNameIndex = managerTypeNames.IndexOf(ComponentType.stringValue);
                if (ComponentTypeNameIndex <= 0)
                {
                    ComponentTypeNameIndex    = 0;
                    ComponentType.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}