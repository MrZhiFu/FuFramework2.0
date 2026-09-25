#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FuMenuPriority = FuFramework.Core.Editor.FuMenuPriority;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Editor
{
    /// <summary>
    /// 事件模块调试面板。
    /// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 EventModule（经 ModuleManager.GetModule 取热更实例）。
    /// 功能：
    ///     1. 模块总览：待分发事件数、订阅事件 ID 数、handler 总数。
    ///     2. 订阅明细：按事件 ID 分组折叠，展示每个 handler 的方法名/所属类型/静态或实例标记，支持跳转源码。
    ///     3. 待分发队列：展示队列中每条事件的 eventId/sender/参数类型，支持一键清空。
    ///     4. 搜索过滤（事件 ID 或 handler 所属类型）、自动刷新、全部展开/折叠。
    /// 数据源全部为 EventModule 公共 API（EventCount/EventHandlerCount/ForEachHandler/ForEachEvent），
    /// 仅「清空待分发队列」经反射取 EventModule 私有字段 m_EventPool 调用池的 Clear（EventModule 未转发该方法）。
    /// 回调绑定：ForEachHandler/ForEachEvent 的回调委托签名为 (string, EventHandler&lt;GameEventArgs&gt;) 与 (object, GameEventArgs)，
    /// 经窗口内泛型适配方法 MakeGenericMethod + CreateDelegate 精确签名绑定，无需动态发射。
    /// </summary>
    public class EventModuleWindow : EditorWindow
    {
        /// <summary>
        /// 打开调试面板
        /// </summary>
        [MenuItem("FuFramework/调试/事件调试面板", false, FuMenuPriority.DEBUG_PANEL_EVENT)]
        public static void ShowWindow()
        {
            var window = GetWindow<EventModuleWindow>("事件调试");
            window.minSize = new Vector2(760, 600);

            // 初始位置居中显示
            const float width  = 900f;
            const float height = 640f;
            var x = (Screen.currentResolution.width  - width)  / 2f;
            var y = (Screen.currentResolution.height - height) / 2f;
            window.position = new Rect(x, y, width, height);
        }

        #region 显示行数据结构

        /// <summary>
        /// 订阅 handler 显示行
        /// </summary>
        private sealed class HandlerRow
        {
            /// <summary>
            /// handler 方法元数据（跳转源码用；仅 Play 会话内有效，快照重建时刷新）
            /// </summary>
            public MethodInfo Method;

            /// <summary>
            /// handler 方法名（含泛型反引号时保持原样）
            /// </summary>
            public string MethodName;

            /// <summary>
            /// 方法所属类型全名
            /// </summary>
            public string DeclaringTypeFullName;

            /// <summary>
            /// 是否静态方法（无目标对象）
            /// </summary>
            public bool IsStatic;

            /// <summary>
            /// 实例 handler 的目标对象类型名（静态为空串）
            /// </summary>
            public string TargetTypeName;
        }

        /// <summary>
        /// 按事件 ID 分组的订阅组
        /// </summary>
        private sealed class HandlerGroup
        {
            /// <summary>
            /// 事件 ID
            /// </summary>
            public string EventId;

            /// <summary>
            /// 该 ID 下的 handler 行
            /// </summary>
            public readonly List<HandlerRow> Rows = new();
        }

        /// <summary>
        /// 待分发队列显示行
        /// </summary>
        private sealed class EventRow
        {
            /// <summary>
            /// 队列序号（从 1 开始）
            /// </summary>
            public int Index;

            /// <summary>
            /// 事件 ID
            /// </summary>
            public string EventId;

            /// <summary>
            /// 发送者类型名（null sender 显示为空串）
            /// </summary>
            public string SenderTypeName;

            /// <summary>
            /// 事件参数类型名
            /// </summary>
            public string ArgsTypeName;
        }

        #endregion

        #region 私有字段

        /// <summary>
        /// 滚动位置
        /// </summary>
        private Vector2 m_ScrollPos;

        /// <summary>
        /// 搜索过滤字符串（匹配事件 ID、handler 方法名或所属类型名）
        /// </summary>
        private string m_SearchFilter = "";

        /// <summary>
        /// 是否自动刷新（每 0.5 秒重建一次快照）
        /// </summary>
        private bool m_AutoRefresh = true;

        /// <summary>
        /// 上次快照重建时间
        /// </summary>
        private double m_LastSnapshotTime;

        /// <summary>
        /// 手动刷新标记（下一次 OnGUI 强制重建快照）
        /// </summary>
        private bool m_ForceSnapshot;

        /// <summary>
        /// 订阅明细段折叠状态
        /// </summary>
        private bool m_HandlerSectionOpen = true;

        /// <summary>
        /// 待分发队列段折叠状态（队列内容变化快，默认折叠避免刷屏）
        /// </summary>
        private bool m_QueueSectionOpen;

        /// <summary>
        /// 各事件 ID 的订阅组折叠状态缓存
        /// </summary>
        private readonly Dictionary<string, bool> m_GroupFoldoutStates = new();

        /// <summary>
        /// 订阅分组快照（按事件 ID 升序）
        /// </summary>
        private readonly List<HandlerGroup> m_HandlerGroups = new();

        /// <summary>
        /// 待分发队列快照
        /// </summary>
        private readonly List<EventRow> m_EventRows = new();

        /// <summary>
        /// 快照构建中的当前分组（回调填充用，避免逐条字典查找开销大的替代方案）
        /// </summary>
        private HandlerGroup m_BuildingGroup;

        #endregion

        #region 反射缓存

        /// <summary>
        /// EventModule 类型
        /// </summary>
        private Type m_ModuleType;

        /// <summary>
        /// EventModule 热更实例
        /// </summary>
        private object m_ModuleInstance;

        /// <summary>
        /// GameEventArgs 类型（构造回调委托的泛型实参）
        /// </summary>
        private Type m_GameEventArgsType;

        /// <summary>
        /// EventModule.EventCount 属性（待分发事件数）
        /// </summary>
        private PropertyInfo m_EventCountProperty;

        /// <summary>
        /// EventModule.EventHandlerCount 属性（handler 总数）
        /// </summary>
        private PropertyInfo m_EventHandlerCountProperty;

        /// <summary>
        /// GameEventArgs.Id 属性（队列行取事件 ID）
        /// </summary>
        private PropertyInfo m_EventIdProperty;

        /// <summary>
        /// EventModule.ForEachHandler 方法（参数 Action&lt;string, EventHandler&lt;GameEventArgs&gt;&gt;）
        /// </summary>
        private MethodInfo m_ForEachHandlerMethod;

        /// <summary>
        /// EventModule.ForEachEvent 方法（参数 Action&lt;object, GameEventArgs&gt;）
        /// </summary>
        private MethodInfo m_ForEachEventMethod;

        /// <summary>
        /// EventModule 私有字段 m_EventPool（清空队列时经池实例调用 Clear）
        /// </summary>
        private FieldInfo m_EventPoolField;

        /// <summary>
        /// 池实例类型的 Clear 方法
        /// </summary>
        private MethodInfo m_PoolClearMethod;

        /// <summary>
        /// 已绑定的 ForEachHandler 回调委托（Action&lt;string, EventHandler&lt;GameEventArgs&gt;&gt;）
        /// </summary>
        private Delegate m_ForEachHandlerCallback;

        /// <summary>
        /// 已绑定的 ForEachEvent 回调委托（Action&lt;object, GameEventArgs&gt;）
        /// </summary>
        private Delegate m_ForEachEventCallback;

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
        /// 编辑器帧更新：定时重绘（快照重建在 OnGUI 中按同一节奏执行）
        /// </summary>
        private void OnEditorUpdate()
        {
            if (!m_AutoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - m_LastSnapshotTime < 0.5f) return;

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
                EditorGUILayout.HelpBox("未能通过反射访问 EventModule，请确认 Hotfix 已加载", MessageType.Warning);
                return;
            }

            // 快照按节奏重建：自动刷新到点或手动刷新标记置位时重建，其余绘制使用缓存快照（折叠交互不触发重建）
            if (m_ForceSnapshot || (m_AutoRefresh && EditorApplication.timeSinceStartup - m_LastSnapshotTime >= 0.5f))
            {
                m_ForceSnapshot      = false;
                m_LastSnapshotTime   = EditorApplication.timeSinceStartup;
                RefreshSnapshots();
            }

            DrawOverview();
            EditorGUILayout.Separator();

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            DrawHandlerSection();
            EditorGUILayout.Separator();
            DrawQueueSection();
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
            m_SearchFilter = GUILayout.TextField(m_SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(180));

            GUILayout.Space(20);
            m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                m_ForceSnapshot = true;
                Repaint();
            }

            if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SetAllGroupFoldouts(true);
            }

            if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SetAllGroupFoldouts(false);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 设置全部订阅组的折叠状态
        /// </summary>
        private void SetAllGroupFoldouts(bool open)
        {
            foreach (var group in m_HandlerGroups)
            {
                m_GroupFoldoutStates[group.EventId] = open;
            }
        }

        #endregion

        #region 模块概览

        /// <summary>
        /// 绘制模块总览条
        /// </summary>
        private void DrawOverview()
        {
            var eventCount        = (int)(m_EventCountProperty?.GetValue(m_ModuleInstance) ?? 0);
            var handlerTotalCount = (int)(m_EventHandlerCountProperty?.GetValue(m_ModuleInstance) ?? 0);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"待分发事件: {eventCount}", GUILayout.MinWidth(110));
            DrawColumnSeparator();
            GUILayout.Label($"订阅事件 ID 数: {m_HandlerGroups.Count}", GUILayout.MinWidth(130));
            DrawColumnSeparator();
            GUILayout.Label($"handler 总数: {handlerTotalCount}", GUILayout.MinWidth(130));
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 订阅明细

        /// <summary>
        /// 绘制订阅明细段
        /// </summary>
        private void DrawHandlerSection()
        {
            var foldoutOldColor = GUI.color;
            GUI.color             = Color.cyan;
            m_HandlerSectionOpen  = EditorGUILayout.Foldout(m_HandlerSectionOpen, $"订阅明细（{m_HandlerGroups.Count} 个事件 ID）", true);
            GUI.color             = foldoutOldColor;
            if (!m_HandlerSectionOpen) return;

            EditorGUILayout.BeginVertical("box");
            foreach (var group in m_HandlerGroups)
            {
                DrawHandlerGroup(group);
            }

            if (m_HandlerGroups.Count == 0)
            {
                EditorGUILayout.HelpBox("当前无任何事件订阅", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制单个事件 ID 的订阅组
        /// </summary>
        /// <param name="group">订阅分组</param>
        private void DrawHandlerGroup(HandlerGroup group)
        {
            // 搜索过滤：事件 ID 或组内任一 handler 匹配才展示
            if (!MatchSearch(group)) return;

            if (!m_GroupFoldoutStates.TryGetValue(group.EventId, out var isOpen))
            {
                isOpen = true;
                m_GroupFoldoutStates[group.EventId] = true;
            }

            m_GroupFoldoutStates[group.EventId] = EditorGUILayout.Foldout(isOpen, $"{group.EventId}  ({group.Rows.Count} 个 handler)", true);
            if (!m_GroupFoldoutStates[group.EventId]) return;

            EditorGUILayout.BeginVertical("box");
            foreach (var row in group.Rows)
            {
                DrawHandlerRow(row);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        /// <summary>
        /// 绘制单个 handler 行：方法名 | 所属类型 | 静态/实例 | 目标对象类型 | [跳转]
        /// </summary>
        private void DrawHandlerRow(HandlerRow row)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(row.MethodName, GUILayout.MinWidth(180));
            DrawColumnSeparator();
            GUILayout.Label(row.DeclaringTypeFullName, GUILayout.MinWidth(200));
            DrawColumnSeparator();
            GUILayout.Label(row.IsStatic ? "静态" : $"实例({row.TargetTypeName})", GUILayout.MinWidth(120));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("跳转", GUILayout.Width(44)))
            {
                OpenHandlerSource(row.Method);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 判断订阅组是否命中搜索过滤：事件 ID、方法名或所属类型名任一包含即命中（大小写不敏感）
        /// </summary>
        private bool MatchSearch(HandlerGroup group)
        {
            if (string.IsNullOrEmpty(m_SearchFilter)) return true;

            if (group.EventId.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase)) return true;

            foreach (var row in group.Rows)
            {
                if (row.MethodName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                if (row.DeclaringTypeFullName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// 跳转到 handler 方法源码：
        ///     1. 按所属类名收集全部候选脚本文件（分部类会有多个，文件名精确一致者优先），
        ///        逐文件扫描「声明形态」的方法行——C# 分部类不允许重复声明同一方法，方法声明在且仅在
        ///        唯一一个分部文件中，故声明行命中即唯一真身；
        ///     2. 无声明行命中时退化为首个含「方法名(」出现的文件行（重载/同名的近似场景）；
        ///     3. 再退化仅打开首个候选文件，找不到任何候选文件时告警。
        /// 说明：曾评估 Mono.Cecil 读调试符号精确映射，但 Unity 安装目录自带 DLL 对 asmdef 程序集不可见
        /// （仅经典 Editor 文件夹自动引用），改造 asmdef 引用方式会影响其它预编译引用，故不采用。
        /// </summary>
        /// <param name="method">handler 方法元数据</param>
        private static void OpenHandlerSource(MethodInfo method)
        {
            if (method == null) return;

            var declaringTypeName = method.DeclaringType?.Name ?? string.Empty;
            var candidatePaths    = FindScriptAssetsByClassName(SimplifyTypeName(declaringTypeName));
            if (candidatePaths.Count == 0)
            {
                Debug.LogWarning($"[事件调试]未找到方法 '{method.DeclaringType?.FullName}.{method.Name}' 对应的源文件.");
                return;
            }

            // 声明行正则：方法名同行前方需存在访问/函数修饰符，天然排除调用点（xxx.MethodName(、var a = MethodName( 等）
            var declarationPattern = new Regex(
                @"\b(public|private|protected|internal|static|async|override|virtual|sealed|new)\b[^;{=]*\b" + Regex.Escape(method.Name) + @"\s*\(",
                RegexOptions.Compiled);

            string declarationPath = null;
            var declarationLine    = 0;
            string occurrencePath  = null;
            var occurrenceLine     = 0;

            foreach (var candidatePath in candidatePaths)
            {
                ScanMethodInFile(candidatePath, method.Name, declarationPattern, out var lineDeclaration, out var lineFirstOccurrence);

                // 声明行命中即唯一真身（分部类不可重复声明），立即采用
                if (lineDeclaration > 0)
                {
                    declarationPath = candidatePath;
                    declarationLine = lineDeclaration;
                    break;
                }

                // 首个含方法名出现的文件记为次选
                if (occurrencePath == null && lineFirstOccurrence > 0)
                {
                    occurrencePath = candidatePath;
                    occurrenceLine = lineFirstOccurrence;
                }
            }

            if (TryOpenAtLine(declarationPath, declarationLine)) return;
            if (TryOpenAtLine(occurrencePath, occurrenceLine)) return;

            // 找不到任何行级定位：仅打开首个候选文件
            var fallbackAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidatePaths[0]);
            if (fallbackAsset != null)
            {
                AssetDatabase.OpenAsset(fallbackAsset);
                return;
            }

            Debug.LogWarning($"[事件调试]未找到方法 '{method.DeclaringType?.FullName}.{method.Name}' 对应的源文件.");
        }

        /// <summary>
        /// 按资产路径打开文件并定位到指定行（行号无效时仅打开文件），资产不存在返回 false
        /// </summary>
        private static bool TryOpenAtLine(string assetPath, int lineNumber)
        {
            if (string.IsNullOrEmpty(assetPath) || lineNumber <= 0) return false;

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null) return false;

            AssetDatabase.OpenAsset(asset, lineNumber);
            return true;
        }

        /// <summary>
        /// 剥离类型名中的泛型元数（如 EventPool`1 的 `1）与嵌套分隔，得到可与文件名/简单名比对的形态
        /// </summary>
        private static string SimplifyTypeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var backtick = name.IndexOf('`');
            var simple   = backtick >= 0 ? name.Substring(0, backtick) : name;
            var plus     = simple.IndexOf('+');
            return plus >= 0 ? simple.Substring(plus + 1) : simple;
        }

        /// <summary>
        /// 按类名在工程内收集候选脚本资产（分部类可能对应多个文件）：
        /// 文件名与类名精确一致的排在最前，其余模糊命中（如 WinBase.EventRegister.cs）按 FindAssets 顺序跟随
        /// </summary>
        private static List<string> FindScriptAssetsByClassName(string className)
        {
            var paths = new List<string>();
            if (string.IsNullOrEmpty(className)) return paths;

            var guids = AssetDatabase.FindAssets($"t:Script {className}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                if (Path.GetFileNameWithoutExtension(path) == className) paths.Insert(0, path);
                else paths.Add(path);
            }

            return paths;
        }

        /// <summary>
        /// 扫描单个源文件：输出「声明形态的方法行」与「方法名( 首次出现行」（均 1 起始，未找到为 0）
        /// </summary>
        private static void ScanMethodInFile(string assetPath, string methodName, Regex declarationPattern, out int declarationLine, out int firstOccurrenceLine)
        {
            declarationLine     = 0;
            firstOccurrenceLine = 0;

            try
            {
                var lines = File.ReadAllLines(Path.GetFullPath(assetPath));
                var token = methodName + "(";
                for (var i = 0; i < lines.Length; i++)
                {
                    // 首次出现行：方法名后紧跟左括号
                    if (firstOccurrenceLine == 0 && lines[i].Contains(token)) firstOccurrenceLine = i + 1;

                    // 声明形态行：命中修饰符前缀正则
                    if (declarationLine == 0 && declarationPattern.IsMatch(lines[i])) declarationLine = i + 1;

                    if (declarationLine > 0 && firstOccurrenceLine > 0) return;
                }
            }
            catch (Exception)
            {
                // 读文件失败按「未找到」处理，由调用方降级
            }
        }

        #endregion

        #region 待分发队列

        /// <summary>
        /// 绘制待分发队列段
        /// </summary>
        private void DrawQueueSection()
        {
            var foldoutOldColor = GUI.color;
            GUI.color             = Color.cyan;
            m_QueueSectionOpen    = EditorGUILayout.Foldout(m_QueueSectionOpen, $"待分发队列（{m_EventRows.Count} 条）", true);
            GUI.color             = foldoutOldColor;
            if (!m_QueueSectionOpen) return;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("清空待分发队列", "调用事件池 Clear：丢弃队列中全部未分发事件（含参数回收）。"), GUILayout.Width(140)))
            {
                ClearPendingEvents();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (m_EventRows.Count == 0)
            {
                EditorGUILayout.LabelField("队列为空");
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("#", GUILayout.Width(40));
                GUILayout.Label("事件 ID", GUILayout.MinWidth(220));
                GUILayout.Label("发送者", GUILayout.MinWidth(160));
                GUILayout.Label("参数类型", GUILayout.MinWidth(200));
                EditorGUILayout.EndHorizontal();

                foreach (var row in m_EventRows)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(row.Index.ToString(), GUILayout.Width(40));
                    GUILayout.Label(row.EventId, GUILayout.MinWidth(220));
                    GUILayout.Label(string.IsNullOrEmpty(row.SenderTypeName) ? "(null)" : row.SenderTypeName, GUILayout.MinWidth(160));
                    GUILayout.Label(row.ArgsTypeName, GUILayout.MinWidth(200));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 清空待分发队列：经反射取 EventModule 私有字段 m_EventPool 调用池的 Clear
        /// （EventModule 未转发 Clear；Clear 会丢弃全部未分发事件并回收其参数，属破坏性调试操作）
        /// </summary>
        private void ClearPendingEvents()
        {
            try
            {
                var pool = m_EventPoolField?.GetValue(m_ModuleInstance);
                if (pool == null)
                {
                    Debug.LogError("[事件调试]获取事件池实例失败，无法清空队列.");
                    return;
                }

                m_PoolClearMethod?.Invoke(pool, null);
                m_ForceSnapshot = true;
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"清空待分发队列失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
            }
        }

        #endregion

        #region 快照构建

        /// <summary>
        /// 重建两份快照：经 ForEachHandler/ForEachEvent 的回调逐条填充
        /// （二者为主线程 API 且自带重入快照保护，OnGUI 主线程调用安全）
        /// </summary>
        private void RefreshSnapshots()
        {
            m_HandlerGroups.Clear();
            m_EventRows.Clear();
            m_BuildingGroup = null;

            try
            {
                // 把回调作为实参传入 ForEachHandler/ForEachEvent（模块内部遍历订阅/队列时逐条回调）：
                // 不能直接 DynamicInvoke 回调本身——它需要 (id, handler)/(sender, eArgs) 实参，那些由模块提供
                m_ForEachHandlerMethod?.Invoke(m_ModuleInstance, new object[] { m_ForEachHandlerCallback });
                m_ForEachEventMethod?.Invoke(m_ModuleInstance, new object[] { m_ForEachEventCallback });
            }
            catch (Exception e)
            {
                Debug.LogError($"[事件调试]构建快照失败:{e.InnerException?.Message ?? e.Message}");
            }

            m_BuildingGroup = null;

            // 订阅组按事件 ID 升序，保证展示顺序稳定
            m_HandlerGroups.Sort((a, b) => string.CompareOrdinal(a.EventId, b.EventId));
        }

        /// <summary>
        /// ForEachHandler 回调的泛型适配（经 MakeGenericMethod 绑定为精确签名委托）：
        /// 把 (id, handler) 折叠进分组快照
        /// </summary>
        private void HandlerEntry<T>(string id, EventHandler<T> handler) where T : EventArgs
        {
            // 事件 ID 变化时切换当前分组（ForEachHandler 按 id 连续枚举同一事件的多 handler，但不依赖该前提：显式查重）
            if (m_BuildingGroup == null || m_BuildingGroup.EventId != id)
            {
                m_BuildingGroup = FindOrAddGroup(id);
            }

            var method = handler.Method;
            m_BuildingGroup.Rows.Add(new HandlerRow
            {
                Method                = method,
                MethodName            = method.Name,
                DeclaringTypeFullName = method.DeclaringType?.FullName ?? "(unknown)",
                IsStatic              = handler.Target == null,
                TargetTypeName        = handler.Target?.GetType().Name ?? string.Empty,
            });
        }

        /// <summary>
        /// ForEachEvent 回调的泛型适配（经 MakeGenericMethod 绑定为精确签名委托）：
        /// 把队列中的 (sender, eArgs) 折叠进行快照
        /// </summary>
        private void EventEntry<T>(object sender, T eArgs) where T : EventArgs
        {
            m_EventRows.Add(new EventRow
            {
                Index          = m_EventRows.Count + 1,
                EventId        = m_EventIdProperty?.GetValue(eArgs) as string ?? "(unknown)",
                SenderTypeName = sender?.GetType().Name ?? string.Empty,
                ArgsTypeName   = eArgs?.GetType().Name ?? "(null)",
            });
        }

        /// <summary>
        /// 查找或建立事件 ID 对应的订阅分组
        /// </summary>
        private HandlerGroup FindOrAddGroup(string id)
        {
            for (var i = 0; i < m_HandlerGroups.Count; i++)
            {
                if (m_HandlerGroups[i].EventId == id) return m_HandlerGroups[i];
            }

            var group = new HandlerGroup { EventId = id };
            m_HandlerGroups.Add(group);
            return group;
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

            m_ModuleType        = Type.GetType("Hotfix.Framework.Event.EventModule, Hotfix");
            m_GameEventArgsType = Type.GetType("Hotfix.Framework.Event.GameEventArgs, Hotfix");
            if (m_ModuleType == null || m_GameEventArgsType == null) return false;

            // EventModule 无静态 Instance 保证，经 ModuleManager.GetModule<T> 泛型方法获取热更实例（ObjectPoolModuleWindow 同款）
            var moduleManagerType = Type.GetType("Hotfix.Framework.Core.ModuleManager, Hotfix");
            if (moduleManagerType == null) return false;

            var getModuleMethod = moduleManagerType.GetMethod("GetModule", BindingFlags.Public | BindingFlags.Static);
            if (getModuleMethod == null) return false;

            m_ModuleInstance = getModuleMethod.MakeGenericMethod(m_ModuleType).Invoke(null, null);
            if (m_ModuleInstance == null) return false;

            // EventModule 成员
            m_EventCountProperty        = m_ModuleType.GetProperty("EventCount",        BindingFlags.Public | BindingFlags.Instance);
            m_EventHandlerCountProperty = m_ModuleType.GetProperty("EventHandlerCount", BindingFlags.Public | BindingFlags.Instance);
            m_ForEachHandlerMethod      = m_ModuleType.GetMethod("ForEachHandler",      BindingFlags.Public | BindingFlags.Instance);
            m_ForEachEventMethod        = m_ModuleType.GetMethod("ForEachEvent",        BindingFlags.Public | BindingFlags.Instance);
            m_EventPoolField            = m_ModuleType.GetField("m_EventPool", BindingFlags.NonPublic | BindingFlags.Instance);

            // GameEventArgs 成员
            m_EventIdProperty = m_GameEventArgsType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

            // 事件池 Clear（清空队列用）
            var eventPoolType = m_EventPoolField?.FieldType;
            m_PoolClearMethod = eventPoolType?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);

            if (m_ForEachHandlerMethod == null || m_ForEachEventMethod == null || m_EventIdProperty == null) return false;

            // 回调绑定：窗口内泛型适配方法 MakeGenericMethod 后 CreateDelegate，签名精确匹配
            var actionOfStringAndHandler = typeof(Action<,>).MakeGenericType(typeof(string), typeof(EventHandler<>).MakeGenericType(m_GameEventArgsType));
            var actionOfObjectAndArgs    = typeof(Action<,>).MakeGenericType(typeof(object), m_GameEventArgsType);

            var handlerAdapter = GetType().GetMethod(nameof(HandlerEntry), BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(m_GameEventArgsType);
            var eventAdapter   = GetType().GetMethod(nameof(EventEntry),   BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(m_GameEventArgsType);
            if (handlerAdapter == null || eventAdapter == null) return false;

            m_ForEachHandlerCallback = Delegate.CreateDelegate(actionOfStringAndHandler, this, handlerAdapter);
            m_ForEachEventCallback   = Delegate.CreateDelegate(actionOfObjectAndArgs, this, eventAdapter);

            if (m_ForEachHandlerCallback == null || m_ForEachEventCallback == null) return false;

            // 绑定后立即重建一次快照，避免首帧展示空数据
            m_ForceSnapshot = true;
            return true;
        }

        /// <summary>
        /// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
        /// </summary>
        private void ResetReflection()
        {
            m_ModuleType              = null;
            m_ModuleInstance          = null;
            m_GameEventArgsType       = null;
            m_EventCountProperty      = null;
            m_EventHandlerCountProperty = null;
            m_EventIdProperty         = null;
            m_ForEachHandlerMethod    = null;
            m_ForEachEventMethod      = null;
            m_EventPoolField          = null;
            m_PoolClearMethod         = null;
            m_ForEachHandlerCallback  = null;
            m_ForEachEventCallback    = null;

            m_HandlerGroups.Clear();
            m_EventRows.Clear();
            m_BuildingGroup = null;
        }

        /// <summary>
        /// 绘制列与列之间的分隔竖线
        /// </summary>
        private static void DrawColumnSeparator()
        {
            GUILayout.Label("|", GUILayout.Width(12));
        }

        #endregion
    }
}
#endif
