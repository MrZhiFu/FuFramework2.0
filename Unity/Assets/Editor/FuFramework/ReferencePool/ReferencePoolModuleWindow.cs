#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ReferencePool.Editor
{
    /// <summary>
    /// 引用池调试面板
    /// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 ReferencePoolModule。
    /// 功能：
    ///     1. 展示所有引用池及其统计信息（类型、闲置、使用中、累计获取/释放/新增/移除）。
    ///     2. 支持按类型名过滤搜索、自动刷新、全部展开/折叠。
    ///     3. 支持模块级/单池级一键清空（清空所有引用池、清空指定类型池）。
    /// </summary>
    public class ReferencePoolModuleWindow : EditorWindow
    {
        /// <summary>
        /// 打开调试面板
        /// </summary>
        [MenuItem("FuFramework/调试/引用池调试面板")]
        public static void ShowWindow()
        {
            var window = GetWindow<ReferencePoolModuleWindow>("引用池调试");
            window.minSize = new Vector2(800, 800);

            // 初始位置居中显示
            const float width  = 1000f;
            const float height = 600f;
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
        /// 引用池折叠状态缓存。ReferencePoolInfo 是 struct，每次查询返回新副本，须以稳定的 Type 为键。
        /// </summary>
        private readonly Dictionary<Type, bool> m_FoldoutStates = new();

        /// <summary>
        /// 上次刷新时间
        /// </summary>
        private double m_LastRefreshTime;

        #endregion

        #region 反射缓存

        /// <summary>
        /// ReferencePoolModule 类型
        /// </summary>
        private Type m_ReferencePoolModuleType;

        /// <summary>
        /// ReferencePoolInfo 类型
        /// </summary>
        private Type m_ReferencePoolInfoType;

        /// <summary>
        /// ReferencePoolModule 实例
        /// </summary>
        private object m_ModuleInstance;

        /// <summary>
        /// ModuleManager.GetModule(Type) 方法
        /// </summary>
        private MethodInfo m_GetModuleMethod;

        /// <summary>
        /// ReferencePoolModule.Count 属性
        /// </summary>
        private PropertyInfo m_ModuleCountProperty;

        /// <summary>
        /// ReferencePoolModule.GetAllReferencePoolInfos 方法
        /// </summary>
        private MethodInfo m_GetAllReferencePoolInfosMethod;

        /// <summary>
        /// ReferencePoolModule.ClearAll 方法
        /// </summary>
        private MethodInfo m_ModuleClearAllMethod;

        /// <summary>
        /// ReferencePoolModule.RemoveAll<T> 泛型方法定义
        /// </summary>
        private MethodInfo m_RemoveAllGenericMethod;

        /// <summary>
        /// ReferencePoolInfo.Type 属性
        /// </summary>
        private PropertyInfo m_InfoTypeProperty;

        /// <summary>
        /// ReferencePoolInfo.UnusedReferenceCount 属性
        /// </summary>
        private PropertyInfo m_InfoUnusedReferenceCountProperty;

        /// <summary>
        /// ReferencePoolInfo.UsingReferenceCount 属性
        /// </summary>
        private PropertyInfo m_InfoUsingReferenceCountProperty;

        /// <summary>
        /// ReferencePoolInfo.AcquireReferenceCount 属性
        /// </summary>
        private PropertyInfo m_InfoAcquireReferenceCountProperty;

        /// <summary>
        /// ReferencePoolInfo.ReleaseReferenceCount 属性
        /// </summary>
        private PropertyInfo m_InfoReleaseReferenceCountProperty;

        /// <summary>
        /// ReferencePoolInfo.AddReferenceCount 属性
        /// </summary>
        private PropertyInfo m_InfoAddReferenceCountProperty;

        /// <summary>
        /// ReferencePoolInfo.RemoveReferenceCount 属性
        /// </summary>
        private PropertyInfo m_InfoRemoveReferenceCountProperty;

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

            // 非 Play 模式：重置反射缓存并提示，避免停止运行后持有已失效的热更实例
            if (!Application.isPlaying)
            {
                ResetReflection();
                EditorGUILayout.HelpBox("需要在 Play 模式下使用", MessageType.Info);
                return;
            }

            if (!EnsureReflection())
            {
                EditorGUILayout.HelpBox("未能通过反射访问 ReferencePoolModule，请确认 Hotfix 已加载", MessageType.Warning);
                return;
            }

            DrawModuleOverview();
            EditorGUILayout.Separator();

            var infos = GetAllReferencePoolInfos();
            if (infos.Count == 0)
            {
                EditorGUILayout.HelpBox("引用池为空", MessageType.Info);
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            foreach (var info in infos)
            {
                DrawReferencePool(info);
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

        #region 模块概览

        /// <summary>
        /// 绘制模块级概览与一键清空操作
        /// </summary>
        private void DrawModuleOverview()
        {
            var count = m_ModuleCountProperty?.GetValue(m_ModuleInstance) ?? 0;
            EditorGUILayout.LabelField($"引用池总个数：{count}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空所有引用池", GUILayout.Width(200)))
            {
                try
                {
                    m_ModuleClearAllMethod?.Invoke(m_ModuleInstance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"清空所有引用池失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 引用池绘制

        /// <summary>
        /// 绘制单个引用池信息
        /// </summary>
        /// <param name="info">引用池信息（ReferencePoolInfo 装箱实例）</param>
        private void DrawReferencePool(object info)
        {
            if (info == null) return;

            var poolType = m_InfoTypeProperty?.GetValue(info) as Type;
            if (poolType == null) return;

            var typeName = poolType.Name;
            var fullName = poolType.FullName ?? typeName;

            // 搜索过滤：类型名或全名匹配才展示
            if (!string.IsNullOrEmpty(m_SearchFilter))
            {
                if (!typeName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase)
                    && !fullName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            if (!m_FoldoutStates.TryGetValue(poolType, out var isOpen))
            {
                isOpen = true;
                m_FoldoutStates[poolType] = true;
            }

            var unusedCount = (int)(m_InfoUnusedReferenceCountProperty?.GetValue(info) ?? 0);
            var usingCount  = (int)(m_InfoUsingReferenceCountProperty?.GetValue(info)    ?? 0);

            // 引用池类型名（Foldout 标题）用青色高亮
            var foldoutOldColor = GUI.color;
            GUI.color             = Color.cyan;
            m_FoldoutStates[poolType] = EditorGUILayout.Foldout(isOpen, $"{typeName} ({usingCount}/{unusedCount})", true);
            GUI.color             = foldoutOldColor;
            if (!m_FoldoutStates[poolType]) return;

            EditorGUILayout.BeginVertical("box");
            {
                DrawPoolStats(info);
                EditorGUILayout.Separator();
                DrawPoolActions(poolType);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        /// <summary>
        /// 绘制引用池统计信息（单行展示）
        /// </summary>
        /// <param name="info">引用池信息（ReferencePoolInfo 装箱实例）</param>
        private void DrawPoolStats(object info)
        {
            var unusedCount  = (int)(m_InfoUnusedReferenceCountProperty?.GetValue(info)   ?? 0);
            var usingCount   = (int)(m_InfoUsingReferenceCountProperty?.GetValue(info)    ?? 0);
            var acquireCount = (int)(m_InfoAcquireReferenceCountProperty?.GetValue(info)  ?? 0);
            var releaseCount = (int)(m_InfoReleaseReferenceCountProperty?.GetValue(info)  ?? 0);
            var addCount     = (int)(m_InfoAddReferenceCountProperty?.GetValue(info)      ?? 0);
            var removeCount  = (int)(m_InfoRemoveReferenceCountProperty?.GetValue(info)   ?? 0);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"闲置: {unusedCount}", GUILayout.MinWidth(80));
            DrawColumnSeparator();
            GUILayout.Label($"使用中: {usingCount}", GUILayout.MinWidth(80));
            DrawColumnSeparator();
            GUILayout.Label($"累计获取: {acquireCount}", GUILayout.MinWidth(100));
            DrawColumnSeparator();
            GUILayout.Label($"累计释放: {releaseCount}", GUILayout.MinWidth(100));
            DrawColumnSeparator();
            GUILayout.Label($"累计新增: {addCount}", GUILayout.MinWidth(100));
            DrawColumnSeparator();
            GUILayout.Label($"累计移除: {removeCount}", GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制列与列之间的分隔竖线
        /// </summary>
        private static void DrawColumnSeparator()
        {
            GUILayout.Label("|", GUILayout.Width(12));
        }

        /// <summary>
        /// 绘制引用池级操作按钮
        /// </summary>
        /// <param name="poolType">引用池类型</param>
        private void DrawPoolActions(Type poolType)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空该类型池", GUILayout.Width(160)))
            {
                try
                {
                    var removeAllMethod = m_RemoveAllGenericMethod?.MakeGenericMethod(poolType);
                    removeAllMethod?.Invoke(m_ModuleInstance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"清空引用池 {poolType.Name} 失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 折叠操作

        /// <summary>
        /// 全部展开
        /// </summary>
        private void ExpandAll()
        {
            var infos = GetAllReferencePoolInfos();
            foreach (var info in infos)
            {
                var poolType = m_InfoTypeProperty?.GetValue(info) as Type;
                if (poolType != null) m_FoldoutStates[poolType] = true;
            }
        }

        /// <summary>
        /// 全部折叠
        /// </summary>
        private void CollapseAll()
        {
            var infos = GetAllReferencePoolInfos();
            foreach (var info in infos)
            {
                var poolType = m_InfoTypeProperty?.GetValue(info) as Type;
                if (poolType != null) m_FoldoutStates[poolType] = false;
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
            if (m_ModuleInstance != null) return true;

            m_ReferencePoolModuleType = Type.GetType("Hotfix.Framework.ReferencePool.ReferencePoolModule, Hotfix");
            if (m_ReferencePoolModuleType == null) return false;

            m_ReferencePoolInfoType = Type.GetType("Hotfix.Framework.ReferencePool.ReferencePoolInfo, Hotfix");
            if (m_ReferencePoolInfoType == null) return false;

            // ReferencePoolModule 没有静态 Instance，通过 ModuleManager.GetModule(Type) 获取热更实例
            var moduleManagerType = Type.GetType("Hotfix.Framework.Core.ModuleManager, Hotfix");
            if (moduleManagerType == null) return false;

            m_GetModuleMethod = moduleManagerType.GetMethod("GetModule", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
            if (m_GetModuleMethod == null) return false;

            m_ModuleInstance = m_GetModuleMethod.Invoke(null, new object[] { m_ReferencePoolModuleType });
            if (m_ModuleInstance == null) return false;

            // ReferencePoolModule 成员
            m_ModuleCountProperty             = m_ReferencePoolModuleType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            m_GetAllReferencePoolInfosMethod  = m_ReferencePoolModuleType.GetMethod("GetAllReferencePoolInfos", BindingFlags.Public | BindingFlags.Instance);
            m_ModuleClearAllMethod            = m_ReferencePoolModuleType.GetMethod("ClearAll", BindingFlags.Public | BindingFlags.Instance);
            m_RemoveAllGenericMethod          = m_ReferencePoolModuleType.GetMethod("RemoveAll", BindingFlags.Public | BindingFlags.Instance);

            // ReferencePoolInfo 成员
            m_InfoTypeProperty                  = m_ReferencePoolInfoType.GetProperty("Type", BindingFlags.Public | BindingFlags.Instance);
            m_InfoUnusedReferenceCountProperty  = m_ReferencePoolInfoType.GetProperty("UnusedReferenceCount", BindingFlags.Public | BindingFlags.Instance);
            m_InfoUsingReferenceCountProperty   = m_ReferencePoolInfoType.GetProperty("UsingReferenceCount", BindingFlags.Public | BindingFlags.Instance);
            m_InfoAcquireReferenceCountProperty = m_ReferencePoolInfoType.GetProperty("AcquireReferenceCount", BindingFlags.Public | BindingFlags.Instance);
            m_InfoReleaseReferenceCountProperty = m_ReferencePoolInfoType.GetProperty("ReleaseReferenceCount", BindingFlags.Public | BindingFlags.Instance);
            m_InfoAddReferenceCountProperty     = m_ReferencePoolInfoType.GetProperty("AddReferenceCount", BindingFlags.Public | BindingFlags.Instance);
            m_InfoRemoveReferenceCountProperty  = m_ReferencePoolInfoType.GetProperty("RemoveReferenceCount", BindingFlags.Public | BindingFlags.Instance);

            return true;
        }

        /// <summary>
        /// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
        /// </summary>
        private void ResetReflection()
        {
            m_ReferencePoolModuleType              = null;
            m_ReferencePoolInfoType                = null;
            m_ModuleInstance                       = null;
            m_GetModuleMethod                      = null;
            m_ModuleCountProperty                  = null;
            m_GetAllReferencePoolInfosMethod       = null;
            m_ModuleClearAllMethod                 = null;
            m_RemoveAllGenericMethod               = null;
            m_InfoTypeProperty                     = null;
            m_InfoUnusedReferenceCountProperty     = null;
            m_InfoUsingReferenceCountProperty      = null;
            m_InfoAcquireReferenceCountProperty    = null;
            m_InfoReleaseReferenceCountProperty    = null;
            m_InfoAddReferenceCountProperty        = null;
            m_InfoRemoveReferenceCountProperty     = null;
        }

        /// <summary>
        /// 获取所有引用池信息。ReferencePoolInfo 是结构体数组，不能协变为 object[]，以 IEnumerable 枚举逐元素装箱。
        /// </summary>
        /// <returns>按类型全名升序排列的引用池信息列表</returns>
        private List<object> GetAllReferencePoolInfos()
        {
            var list = new List<object>();
            var result = m_GetAllReferencePoolInfosMethod?.Invoke(m_ModuleInstance, null) as IEnumerable;
            if (result == null) return list;

            foreach (var item in result)
            {
                if (item != null) list.Add(item);
            }

            // 按类型全名升序排列（引用池无优先级概念）
            list.Sort((a, b) =>
            {
                var typeA = m_InfoTypeProperty?.GetValue(a) as Type;
                var typeB = m_InfoTypeProperty?.GetValue(b) as Type;
                return string.CompareOrdinal(typeA?.FullName ?? "", typeB?.FullName ?? "");
            });

            return list;
        }

        #endregion
    }
}
#endif
