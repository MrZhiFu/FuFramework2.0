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
        [MenuItem("FuFramework/调试/红点调试面板")]
        public static void ShowWindow()
        {
            GetWindow<RedDotDebugWindow>("红点调试");
        }

        private Vector2 mScrollPos;
        private string  mSearchFilter = "";
        private bool    mAutoRefresh  = true;
        private bool    mShowTreeView = true;
        private readonly Dictionary<object, bool> mFoldoutStates = new();
        private double  mLastRefreshTime;

        private Type    mModuleType;
        private object  mModuleInstance;
        private MethodInfo mGetAllNodesMethod;
        private MethodInfo mGetStateMethod;
        private MethodInfo mMarkReadMethod;
        private MethodInfo mGetChildrenMethod;
        private PropertyInfo mKeyProperty;
        private PropertyInfo mParentProperty;
        private PropertyInfo mRawCountProperty;
        private PropertyInfo mTotalCountProperty;
        private PropertyInfo mIsActiveProperty;
        private PropertyInfo mIsReadProperty;
        private PropertyInfo mIsDirtyProperty;
        private PropertyInfo mLogicTypeProperty;
        private PropertyInfo mCleanStrategyProperty;
        private PropertyInfo mDisplayModeProperty;
        private PropertyInfo mCalculatorProperty;
        private PropertyInfo mTriggerEventsProperty;
        private PropertyInfo mIsStaticProperty;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!mAutoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - mLastRefreshTime < 0.5f) return;

            mLastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

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

            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos);

            if (mShowTreeView)
            {
                foreach (var node in nodes)
                {
                    var parent = GetParent(node);
                    if (parent == null)
                        DrawNodeTree(node, 0);
                }
            }
            else
            {
                DrawFlatList(nodes);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("搜索:", GUILayout.Width(40));
            mSearchFilter = GUILayout.TextField(mSearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));

            GUILayout.Space(20);
            mShowTreeView = GUILayout.Toggle(mShowTreeView, "树形", EditorStyles.toolbarButton, GUILayout.Width(60));
            mAutoRefresh  = GUILayout.Toggle(mAutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Repaint();

            if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ExpandAll();

            if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(80)))
                CollapseAll();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeTree(object node, int indentLevel)
        {
            if (node == null) return;

            var keyString = GetKeyString(node);
            if (!string.IsNullOrEmpty(mSearchFilter))
            {
                if (!keyString.Contains(mSearchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    bool childMatches = false;
                    foreach (var child in GetChildren(node))
                    {
                        if (GetKeyString(child).Contains(mSearchFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            childMatches = true;
                            break;
                        }
                    }

                    if (!childMatches) return;
                }
            }

            var children = GetChildren(node);
            bool hasChildren = children.Any();
            var totalCount = (int)(mTotalCountProperty?.GetValue(node) ?? 0);
            var rawCount   = (int)(mRawCountProperty?.GetValue(node) ?? 0);
            var isActive   = (bool)(mIsActiveProperty?.GetValue(node) ?? true);
            var isRead     = (bool)(mIsReadProperty?.GetValue(node) ?? false);
            var isDirty    = (bool)(mIsDirtyProperty?.GetValue(node) ?? false);
            var isStatic   = (bool)(mIsStaticProperty?.GetValue(node) ?? false);
            var logicType  = mLogicTypeProperty?.GetValue(node)?.ToString() ?? "-";
            var cleanStrategy = mCleanStrategyProperty?.GetValue(node)?.ToString() ?? "-";
            var displayMode   = mDisplayModeProperty?.GetValue(node)?.ToString() ?? "-";
            var hasCalculator = mCalculatorProperty?.GetValue(node) != null;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indentLevel * 20);

            if (hasChildren)
            {
                if (!mFoldoutStates.TryGetValue(node, out bool foldout))
                {
                    foldout = true;
                    mFoldoutStates[node] = foldout;
                }

                mFoldoutStates[node] = EditorGUILayout.Foldout(foldout, $"{keyString}", true);
            }
            else
            {
                GUILayout.Label($"{keyString}", GUILayout.Width(180));
            }

            var typeIcon = isStatic ? "S" : "D";
            GUILayout.Label(typeIcon, GUILayout.Width(20));

            var oldColor = GUI.color;
            GUI.color = isActive ? Color.green : Color.gray;
            GUILayout.Label(isActive ? "●" : "○", GUILayout.Width(20));
            GUI.color = oldColor;

            GUILayout.Label($"最终: {totalCount}", GUILayout.Width(60));
            GUILayout.Label($"原始: {rawCount}", GUILayout.Width(60));
            GUILayout.Label($"{logicType}", GUILayout.Width(40));
            GUILayout.Label($"{displayMode}", GUILayout.Width(40));
            GUILayout.Label($"{cleanStrategy}", GUILayout.Width(60));

            if (isRead)
                GUILayout.Label("已读", GUILayout.Width(40));
            if (isDirty)
                GUILayout.Label("脏", GUILayout.Width(30));
            if (hasCalculator)
                GUILayout.Label("Calc", GUILayout.Width(40));

            if (isStatic && GUILayout.Button("已读", GUILayout.Width(50)))
            {
                var key = mKeyProperty?.GetValue(node);
                if (key != null)
                    mMarkReadMethod?.Invoke(mModuleInstance, new[] { key });
            }

            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                var key = mKeyProperty?.GetValue(node);
                if (key != null)
                    mGetStateMethod?.Invoke(mModuleInstance, new[] { key });
            }

            EditorGUILayout.EndHorizontal();

            if (hasChildren && mFoldoutStates.TryGetValue(node, out bool open) && open)
            {
                foreach (var child in children)
                    DrawNodeTree(child, indentLevel + 1);
            }
        }

        private void DrawFlatList(List<object> nodes)
        {
            foreach (var node in nodes)
            {
                var keyString = GetKeyString(node);
                if (!string.IsNullOrEmpty(mSearchFilter) &&
                    !keyString.Contains(mSearchFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                DrawNodeTree(node, 0);
            }
        }

        private void ExpandAll()
        {
            var nodes = GetAllNodes();
            if (nodes == null) return;
            foreach (var node in nodes)
                mFoldoutStates[node] = true;
        }

        private void CollapseAll()
        {
            var nodes = GetAllNodes();
            if (nodes == null) return;
            foreach (var node in nodes)
                mFoldoutStates[node] = false;
        }

        private bool EnsureReflection()
        {
            if (mModuleType != null && mModuleInstance != null) return true;

            mModuleType = Type.GetType("Hotfix.Framework.RedDot.RedDotModule, Hotfix");
            if (mModuleType == null) return false;

            var instanceProperty = mModuleType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            mModuleInstance = instanceProperty?.GetValue(null);
            if (mModuleInstance == null) return false;

            var nodeType = Type.GetType("Hotfix.Framework.RedDot.RedDotNode, Hotfix");
            if (nodeType == null) return false;

            mGetAllNodesMethod = mModuleType.GetMethod("GetAllNodes", BindingFlags.Public | BindingFlags.Instance);
            mGetStateMethod    = mModuleType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Instance, null, new[] { Type.GetType("Hotfix.Framework.RedDot.RedDotKey, Hotfix") }, null);
            mMarkReadMethod    = mModuleType.GetMethod("MarkRead", BindingFlags.Public | BindingFlags.Instance, null, new[] { Type.GetType("Hotfix.Framework.RedDot.RedDotKey, Hotfix") }, null);
            mGetChildrenMethod = nodeType.GetMethod("GetChildren", BindingFlags.Public | BindingFlags.Instance);

            mKeyProperty            = nodeType.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
            mParentProperty         = nodeType.GetProperty("Parent", BindingFlags.Public | BindingFlags.Instance);
            mRawCountProperty       = nodeType.GetProperty("RawCount", BindingFlags.Public | BindingFlags.Instance);
            mTotalCountProperty     = nodeType.GetProperty("TotalCount", BindingFlags.Public | BindingFlags.Instance);
            mIsActiveProperty       = nodeType.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);
            mIsReadProperty         = nodeType.GetProperty("IsRead", BindingFlags.Public | BindingFlags.Instance);
            mIsDirtyProperty        = nodeType.GetProperty("IsDirty", BindingFlags.Public | BindingFlags.Instance);
            mLogicTypeProperty      = nodeType.GetProperty("LogicType", BindingFlags.Public | BindingFlags.Instance);
            mCleanStrategyProperty  = nodeType.GetProperty("CleanStrategy", BindingFlags.Public | BindingFlags.Instance);
            mDisplayModeProperty    = nodeType.GetProperty("DisplayMode", BindingFlags.Public | BindingFlags.Instance);
            mCalculatorProperty     = nodeType.GetProperty("Calculator", BindingFlags.Public | BindingFlags.Instance);
            mTriggerEventsProperty  = nodeType.GetProperty("TriggerEvents", BindingFlags.Public | BindingFlags.Instance);
            mIsStaticProperty       = nodeType.GetProperty("IsStatic", BindingFlags.Public | BindingFlags.Instance);

            return true;
        }

        private List<object> GetAllNodes()
        {
            var result = mGetAllNodesMethod?.Invoke(mModuleInstance, null);
            if (result is IEnumerable<object> enumerable) return enumerable.ToList();
            return (result as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
        }

        private object GetParent(object node) => mParentProperty?.GetValue(node);

        private IEnumerable<object> GetChildren(object node)
        {
            var value = mGetChildrenMethod?.Invoke(node, null);
            if (value is IEnumerable<object> enumerable) return enumerable;
            return (value as IEnumerable)?.Cast<object>() ?? Enumerable.Empty<object>();
        }

        private string GetKeyString(object node)
        {
            var key = mKeyProperty?.GetValue(node);
            return key?.ToString() ?? "<null>";
        }
    }
}
#endif
