#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.RedDot.Editor
{
    /// <summary>
    /// 红点调试面板
    /// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 RedDotModule。
    /// </summary>
    public class RedDotDebugWindow : EditorWindow
    {
        /// <summary>
        /// 打开调试面板
        /// </summary>
        [MenuItem("FuFramework/调试/红点调试面板")]
        public static void ShowWindow()
        {
            var window = GetWindow<RedDotDebugWindow>("红点调试");

            // 初始位置居中显示，初始宽高 1000x800
            const float width  = 1000f;
            const float height = 800f;
            var x = (Screen.currentResolution.width  - width)  / 2f;
            var y = (Screen.currentResolution.height - height) / 2f;
            window.position = new Rect(x, y, width, height);
        }

        #region 私有字段

        /// <summary>
        /// 滚动位置
        /// </summary>
        private Vector2 m_ScrollPos;

        /// <summary>
        /// 搜索过滤字符串
        /// </summary>
        private string m_SearchFilter = "";

        /// <summary>
        /// 是否自动刷新
        /// </summary>
        private bool m_AutoRefresh = true;

        /// <summary>
        /// 节点折叠状态缓存
        /// </summary>
        private readonly Dictionary<object, bool> m_FoldoutStates = new();

        /// <summary>
        /// 上次刷新时间
        /// </summary>
        private double m_LastRefreshTime;

        #endregion

        #region 反射缓存

        /// <summary>
        /// RedDotModule 类型
        /// </summary>
        private Type m_ModuleType;

        /// <summary>
        /// RedDotModule 实例
        /// </summary>
        private object m_ModuleInstance;

        /// <summary>
        /// 获取所有节点的方法
        /// </summary>
        private MethodInfo m_GetAllNodesMethod;

        /// <summary>
        /// 获取节点状态的方法
        /// </summary>
        private MethodInfo m_GetStateMethod;

        /// <summary>
        /// 标记节点为已读的方法
        /// </summary>
        private MethodInfo m_MarkReadMethod;

        /// <summary>
        /// 获取子节点的方法
        /// </summary>
        private MethodInfo m_GetChildrenMethod;

        /// <summary>
        /// 节点Key属性
        /// </summary>
        private PropertyInfo m_KeyProperty;

        /// <summary>
        /// 父节点属性
        /// </summary>
        private PropertyInfo m_ParentProperty;

        /// <summary>
        /// 节点的原始计数（自身，不含子节点）
        /// </summary>
        private PropertyInfo m_RawCountProperty;

        /// <summary>
        /// 节点的总计数（自身 + 所有子节点）
        /// </summary>
        private PropertyInfo m_TotalCountProperty;

        /// <summary>
        /// 是否是静态节点属性
        /// </summary>
        private PropertyInfo m_IsStaticProperty;

        /// <summary>
        /// 节点是否激活属性
        /// </summary>
        private PropertyInfo m_IsActiveProperty;

        /// <summary>
        /// 节点是否已读属性
        /// </summary>
        private PropertyInfo m_IsReadProperty;

        /// <summary>
        /// 节点脏标记属性
        /// </summary>
        private PropertyInfo m_IsDirtyProperty;

        /// <summary>
        /// 子节点聚合逻辑属性
        /// </summary>
        private PropertyInfo m_LogicTypeProperty;

        /// <summary>
        /// 节点清理策略属性
        /// </summary>
        private PropertyInfo m_CleanStrategyProperty;

        /// <summary>
        /// 显示模式属性
        /// </summary>
        private PropertyInfo m_DisplayModeProperty;

        /// <summary>
        /// 计算逻辑属性
        /// </summary>
        private PropertyInfo m_CalculatorProperty;

        /// <summary>
        /// 触发重算的事件ID列表属性
        /// </summary>
        private PropertyInfo m_TriggerEventsProperty;

        #endregion

        #region 生命周期

        /// <summary>
        /// 启用：订阅 EditorApplication.update
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 禁用：取消订阅 EditorApplication.update
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 编辑器帧更新：定时重绘
        /// </summary>
        private void OnEditorUpdate()
        {
            if (!m_AutoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - m_LastRefreshTime < 0.5f) return;

            m_LastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        /// <summary>
        /// 绘制 GUI
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要在 Play 模式下使用", MessageType.Info);
                return;
            }

            if (!EnsureReflection())
            {
                EditorGUILayout.HelpBox("未能通过反射访问 RedDotModule，请确认 Hotfix 已加载", MessageType.Warning);
                return;
            }

            var nodes = GetAllNodes();
            if (nodes == null || nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("红点树为空", MessageType.Info);
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            // 绘制节点树
            foreach (var node in nodes)
            {
                if (GetParent(node) == null)
                {
                    DrawNodeTree(node, 0);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 工具栏

        /// <summary>
        /// 绘制顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("搜索:", GUILayout.Width(40));
            m_SearchFilter = GUILayout.TextField(m_SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));

            GUILayout.Space(20);
            m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }

            if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ExpandAll();
            }

            if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CollapseAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 节点绘制

        /// <summary>
        /// 递归绘制节点树
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="indentLevel">缩进层级</param>
        private void DrawNodeTree(object node, int indentLevel)
        {
            if (node == null) return;

            // 检查是否需要过滤
            var keyString = GetKeyString(node);
            if (!string.IsNullOrEmpty(m_SearchFilter))
            {
                if (!keyString.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    // 如果当前节点不匹配，检查子节点是否匹配
                    bool childMatches = false;
                    foreach (var child in GetChildren(node))
                    {
                        if (!GetKeyString(child).Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase)) continue;
                        childMatches = true;
                        break;
                    }

                    if (!childMatches) return;
                }
            }

            // 获取节点的所有子节点
            var children = GetChildren(node);

            var enumerable  = children as object[] ?? children.ToArray();
            var hasChildren = enumerable.Any();

            var totalCount = (int)(m_TotalCountProperty?.GetValue(node) ?? 0);
            var rawCount   = (int)(m_RawCountProperty?.GetValue(node)   ?? 0);
            var isActive   = (bool)(m_IsActiveProperty?.GetValue(node)  ?? true);
            var isRead     = (bool)(m_IsReadProperty?.GetValue(node)    ?? false);
            var isDirty    = (bool)(m_IsDirtyProperty?.GetValue(node)   ?? false);
            var isStatic   = (bool)(m_IsStaticProperty?.GetValue(node)  ?? false);

            var logicType     = m_LogicTypeProperty?.GetValue(node)?.ToString()     ?? "-";
            var cleanStrategy = m_CleanStrategyProperty?.GetValue(node)?.ToString() ?? "-";
            var displayMode   = m_DisplayModeProperty?.GetValue(node)?.ToString()   ?? "-";

            var hasCalculator = m_CalculatorProperty?.GetValue(node) != null;

            var triggerEvents    = (string[])m_TriggerEventsProperty?.GetValue(node);
            var triggerEventsStr = triggerEvents is { Length: > 0 } ? string.Join(",", triggerEvents) : "";

            EditorGUILayout.BeginHorizontal();

            // Key 区域：固定宽度子 Horizontal，缩进在内部，所有层级信息列起点一致
            const int keyAreaWidth = 260;
            EditorGUILayout.BeginHorizontal(GUILayout.Width(keyAreaWidth));
            GUILayout.Space(indentLevel * 20);
            if (hasChildren)
            {
                if (!m_FoldoutStates.TryGetValue(node, out var foldout))
                {
                    foldout               = true;
                    m_FoldoutStates[node] = true;
                }

                m_FoldoutStates[node] = EditorGUILayout.Foldout(foldout, keyString, true);
            }
            else
            {
                GUILayout.Label(keyString);
            }

            EditorGUILayout.EndHorizontal();

            // 节点信息列：所有行的以下列宽固定，靠左排列
            GUILayout.Label(isStatic ? "静态" : "动态", GUILayout.Width(40));

            var oldColor = GUI.color;
            GUI.color = isActive ? Color.green : Color.gray;
            GUILayout.Label(isActive ? "激活" : "未激活", GUILayout.Width(50));
            GUI.color = oldColor;

            GUILayout.Label($"最终计数: {totalCount}",    GUILayout.Width(80));
            GUILayout.Label($"原始计数: {rawCount}",      GUILayout.Width(80));
            GUILayout.Label($"聚合逻辑: {logicType}",     GUILayout.Width(90));
            GUILayout.Label($"显示模式: {displayMode}",   GUILayout.Width(130));
            GUILayout.Label($"清理策略: {cleanStrategy}", GUILayout.Width(110));

            if (isRead)
                GUILayout.Label("已读", GUILayout.Width(40));
            if (isDirty)
                GUILayout.Label("脏", GUILayout.Width(30));
            if (hasCalculator)
                GUILayout.Label("有计算函数", GUILayout.Width(80));
            if (!string.IsNullOrEmpty(triggerEventsStr))
                GUILayout.Label($"触发重算事件: {triggerEventsStr}", GUILayout.Width(200));

            // 操作按钮：设置为已读
            if (isStatic && GUILayout.Button("已读", GUILayout.Width(50)))
            {
                var key = m_KeyProperty?.GetValue(node);
                if (key != null)
                    m_MarkReadMethod?.Invoke(m_ModuleInstance, new[] { key });
            }

            // 操作按钮：刷新状态
            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                var key = m_KeyProperty?.GetValue(node);
                if (key != null)
                    m_GetStateMethod?.Invoke(m_ModuleInstance, new[] { key });
            }

            EditorGUILayout.EndHorizontal();

            // 折叠/展开子节点
            if (hasChildren && m_FoldoutStates.TryGetValue(node, out var open) && open)
            {
                foreach (var child in enumerable)
                {
                    DrawNodeTree(child, indentLevel + 1);
                }
            }
        }

        /// <summary>
        /// 全部展开
        /// </summary>
        private void ExpandAll()
        {
            var nodes = GetAllNodes();
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                m_FoldoutStates[node] = true;
            }
        }

        /// <summary>
        /// 全部折叠
        /// </summary>
        private void CollapseAll()
        {
            var nodes = GetAllNodes();
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                m_FoldoutStates[node] = false;
            }
        }

        #endregion

        #region 反射

        /// <summary>
        /// 确保反射缓存已初始化
        /// </summary>
        /// <returns>初始化成功返回 true</returns>
        private bool EnsureReflection()
        {
            if (m_ModuleType != null && m_ModuleInstance != null) return true;

            m_ModuleType = Type.GetType("Hotfix.Framework.RedDot.RedDotModule, Hotfix");
            if (m_ModuleType == null) return false;

            var instanceProperty = m_ModuleType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            m_ModuleInstance = instanceProperty?.GetValue(null);
            if (m_ModuleInstance == null) return false;

            var nodeType = Type.GetType("Hotfix.Framework.RedDot.RedDotNode, Hotfix");
            if (nodeType == null) return false;

            var keyType = Type.GetType("Hotfix.Framework.RedDot.RedDotKey, Hotfix");
            if (keyType == null) return false;

            m_GetAllNodesMethod = m_ModuleType.GetMethod("GetAllNodes", BindingFlags.Public | BindingFlags.Instance);
            m_GetStateMethod    = m_ModuleType.GetMethod("GetState",    BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType }, null);
            m_MarkReadMethod    = m_ModuleType.GetMethod("MarkRead",    BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType }, null);

            m_GetChildrenMethod = nodeType.GetMethod("GetChildren", BindingFlags.Public | BindingFlags.Instance);

            m_KeyProperty           = nodeType.GetProperty("Key",           BindingFlags.Public | BindingFlags.Instance);
            m_ParentProperty        = nodeType.GetProperty("Parent",        BindingFlags.Public | BindingFlags.Instance);
            m_RawCountProperty      = nodeType.GetProperty("RawCount",      BindingFlags.Public | BindingFlags.Instance);
            m_TotalCountProperty    = nodeType.GetProperty("TotalCount",    BindingFlags.Public | BindingFlags.Instance);
            m_IsActiveProperty      = nodeType.GetProperty("IsActive",      BindingFlags.Public | BindingFlags.Instance);
            m_IsReadProperty        = nodeType.GetProperty("IsRead",        BindingFlags.Public | BindingFlags.Instance);
            m_IsDirtyProperty       = nodeType.GetProperty("IsDirty",       BindingFlags.Public | BindingFlags.Instance);
            m_LogicTypeProperty     = nodeType.GetProperty("LogicType",     BindingFlags.Public | BindingFlags.Instance);
            m_CleanStrategyProperty = nodeType.GetProperty("CleanStrategy", BindingFlags.Public | BindingFlags.Instance);
            m_DisplayModeProperty   = nodeType.GetProperty("DisplayMode",   BindingFlags.Public | BindingFlags.Instance);
            m_CalculatorProperty    = nodeType.GetProperty("Calculator",    BindingFlags.Public | BindingFlags.Instance);
            m_TriggerEventsProperty = nodeType.GetProperty("TriggerEvents", BindingFlags.Public | BindingFlags.Instance);
            m_IsStaticProperty      = nodeType.GetProperty("IsStatic",      BindingFlags.Public | BindingFlags.Instance);

            return true;
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        /// <returns>所有红点节点列表</returns>
        private List<object> GetAllNodes()
        {
            var result = m_GetAllNodesMethod?.Invoke(m_ModuleInstance, null);
            if (result is IEnumerable<object> enumerable) return enumerable.ToList();
            return (result as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
        }

        /// <summary>
        /// 获取节点父节点
        /// </summary>
        /// <param name="node">目标节点</param>
        /// <returns>父节点，无父节点时返回 null</returns>
        private object GetParent(object node) => m_ParentProperty?.GetValue(node);

        /// <summary>
        /// 获取节点的所有子节点
        /// </summary>
        /// <param name="node">目标节点</param>
        /// <returns>子节点集合</returns>
        private IEnumerable<object> GetChildren(object node)
        {
            var value = m_GetChildrenMethod?.Invoke(node, null);
            if (value is IEnumerable<object> enumerable) return enumerable;
            return (value as IEnumerable)?.Cast<object>() ?? Enumerable.Empty<object>();
        }

        /// <summary>
        /// 获取节点的 Key 字符串
        /// </summary>
        /// <param name="node">目标节点</param>
        /// <returns>Key 字符串，无法获取时返回 &lt;null&gt;</returns>
        private string GetKeyString(object node)
        {
            var key = m_KeyProperty?.GetValue(node);
            return key?.ToString() ?? "<null>";
        }

        #endregion
    }
}
#endif