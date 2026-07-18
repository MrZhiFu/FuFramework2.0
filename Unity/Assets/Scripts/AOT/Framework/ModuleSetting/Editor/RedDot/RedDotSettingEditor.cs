#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.ModuleSetting.Runtime.RedDot;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor.RedDot
{
    /// <summary>
    /// 红点模块配置Inspector编辑器
    /// 
    /// 功能特性：
    /// 1. 层级结构管理 - 完整的树形节点编辑，支持根节点和子节点的添加、删除、展开/折叠
    /// 2. 格式验证系统 - 严格的Key格式验证，确保符合"父节点.子节点"的层级规范
    /// 3. 重复Key检测 - 自动检测并提示重复的Key，确保所有Key的唯一性
    /// 4. 智能错误定位 - 一键定位到问题节点，快速修复格式错误或重复Key
    /// 5. 自动代码生成 - 保存时自动导出静态类代码，供运行时安全使用
    /// 6. 友好的用户体验 - 清晰的错误提示、颜色标识和操作指引
    /// 
    /// Key格式规范：
    /// - 根节点：单级名称，不能包含点号(.)，如 "Main", "System"
    /// - 子节点：必须符合"父节点.子节点"格式，如 "Main.Mail", "Main.Mail.Unread"
    /// - 字符限制：只能包含英文字母(A-Z, a-z)和点号(.)
    /// - 格式要求：不能以点号开头或结尾，不能包含连续的点号
    /// 
    /// 使用流程：
    /// 1. 添加根节点和子节点构建红点树结构
    /// 2. 点击保存按钮进行格式验证和重复检查
    /// 3. 根据错误提示修复问题节点
    /// 4. 成功保存后自动生成静态类代码
    /// 5. 在代码中使用 RedDotKeys.xxx 常量访问红点Key
    /// 
    /// 输出文件：
    /// - 静态类代码自动导出到：Assets/Scripts/Hotfix/RedDot/RedDotKeys.cs
    /// </summary>
    [CustomEditor(typeof(RedDotSetting))]
    public partial class RedDotSettingEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 根节点列表属性
        /// </summary>
        private SerializedProperty m_RootNodesProperty;

        /// <summary>
        /// 用于跟踪节点展开状态的字典
        /// </summary>
        private readonly Dictionary<RedDotNodeData, bool> m_NodeExpanded = new();

        /// <summary>
        /// 存储重复key及其对应的节点路径和节点引用列表
        /// </summary>
        private Dictionary<string, List<(string path, RedDotNodeData node)>> m_DuplicateKeyPaths = new();

        /// <summary>
        /// 存储格式错误的节点及其错误信息
        /// </summary>
        private List<(RedDotNodeData node, string path, string error)> m_InvalidFormatNodes = new();

        /// <summary>
        /// 需要聚焦的节点引用
        /// </summary>
        private RedDotNodeData m_FocusNode;

        /// <summary>
        /// 需要聚焦的节点Key
        /// </summary>
        private string m_FocusNodeKey;

        /// <summary>
        /// 是否需要在下一帧聚焦
        /// </summary>
        private bool m_NeedFocusNextFrame;

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_RootNodesProperty = serializedObject.FindProperty("m_RootNodes");
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("红点节点树配置", EditorStyles.boldLabel);

            // 显示格式错误信息
            if (m_InvalidFormatNodes.Count > 0)
            {
                DrawInvalidFormatWarning();
            }

            // 显示重复key错误信息
            if (m_DuplicateKeyPaths.Count > 0)
            {
                DrawDuplicateKeysWarning();
            }

            // 绘制根节点列表
            if (m_RootNodesProperty != null)
            {
                DrawNodeListProperty(m_RootNodesProperty, 0);
            }
            else
            {
                EditorGUILayout.HelpBox("未找到根节点属性", MessageType.Error);
            }

            // 添加创建根节点的按钮 - 绿色
            GUILayout.Space(10);
            using (new BackgroundColorScope(Color.green))
            {
                if (GUILayout.Button("添加根节点", GUILayout.Height(30)))
                {
                    AddRootNode();
                }
            }

            // 保存按钮 - 黄色（替换原来的检查重复Key按钮）
            GUILayout.Space(15);
            using (new BackgroundColorScope(Color.yellow))
            {
                if (GUILayout.Button("保存", GUILayout.Height(35)))
                {
                    SaveConfiguration();
                }
            }

            // 操作提示
            GUILayout.Space(20);
            EditorGUILayout.HelpBox("保存操作流程：\n"                                                +
                                    "1. 检查重复Key - 确保所有Key唯一性\n"                                +
                                    "2. 验证格式规范 - 检查Key格式是否符合层级要求\n"                            +
                                    "3. 保存配置文件 - 保存红点配置数据\n"                                   +
                                    "4. 导出静态类 - 自动生成 RedDotKeys.cs 代码文件\n\n"                   +
                                    
                                    "Key格式规范：\n"                                               +
                                    "1. 根节点：单级名称，不能包含点号，如 'Main', 'System'\n"                   +
                                    "2. 子节点：必须为'父节点.子节点'格式，如 'Main.Mail', 'Main.Mail.Unread'\n" +
                                    "3. 字符限制：只能包含英文字母(A-Z, a-z)和点号(.)\n"                        +
                                    "4. 格式要求：不能以点号开头/结尾，不能包含连续点号\n\n"                           +
                                    "key代码输出位置：Assets/Scripts/Hotfix/RedDot/RedDotKeys.cs",
                                    MessageType.Info);

            serializedObject.ApplyModifiedProperties();

            // 处理焦点节点
            if (m_NeedFocusNextFrame && m_FocusNode != null && !string.IsNullOrEmpty(m_FocusNodeKey))
            {
                HandleFocusNextFrame();
            }
        }

        /// <summary>
        /// 当Inspector被销毁时清理字典
        /// </summary>
        private void OnDestroy()
        {
            m_NodeExpanded.Clear();
            m_DuplicateKeyPaths.Clear();
            m_InvalidFormatNodes.Clear();
        }
    }
}
#endif