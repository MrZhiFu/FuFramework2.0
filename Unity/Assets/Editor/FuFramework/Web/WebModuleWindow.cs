#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Editor
{
    /// <summary>
    /// Web 模块调试面板
    /// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 WebModule 调试埋点。
    /// 功能：
    ///     1. 模块概览：累计计数（发起/成功/失败/超时/取消）、收发字节与速率、并发占用，超时/并发上限实时调整。
    ///     2. 实时请求：展示当前等待/发送中的 JSON 与 Pb 请求，支持按 URL 过滤，调用方已取消的滞留请求红色告警。
    ///     3. 最近请求记录：环形保留最近 200 条终结摘要，可按结果类型与关键字过滤；每行可复制 URL，POST 记录可展开查看/复制请求体。
    ///     4. 操作：重置统计/清空记录、清空等待并取消在途请求（急救，需二次确认）。
    ///     5. 手动测试请求：输入 URL、GET/POST 切换（POST 可填 JSON 请求体）直接发请求，展示响应/错误，并自动进入最近请求记录。
    /// </summary>
    public class WebModuleWindow : EditorWindow
    {
        /// <summary>
        /// 打开调试面板
        /// </summary>
        [MenuItem("FuFramework/调试/Web模块调试面板")]
        public static void ShowWindow()
        {
            var window = GetWindow<WebModuleWindow>("Web模块调试");
            window.minSize = new Vector2(900, 600);

            // 初始位置居中显示
            const float width  = 1100f;
            const float height = 700f;
            var         x      = (Screen.currentResolution.width  - width)  / 2f;
            var         y      = (Screen.currentResolution.height - height) / 2f;
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
        /// 上次刷新时间
        /// </summary>
        private double m_LastRefreshTime;

        /// <summary>
        /// 最近记录结果类型过滤（依次为 成功/超时/失败/取消）
        /// </summary>
        private readonly bool[] m_ResultFilters = { true, true, true, true };

        /// <summary>
        /// 超时输入框内容（秒）
        /// </summary>
        private string m_TimeoutText = "5";

        /// <summary>
        /// 并发上限输入框内容
        /// </summary>
        private string m_MaxConnText = "8";

        /// <summary>
        /// 手动测试请求方法（0 GET / 1 POST）
        /// </summary>
        private int m_ManualMethod;

        /// <summary>
        /// 手动测试请求 URL
        /// </summary>
        private string m_ManualUrl = "";

        /// <summary>
        /// 手动测试请求的 POST JSON 请求体
        /// </summary>
        private string m_ManualBody = "";

        /// <summary>
        /// 实时请求折叠状态缓存（以请求数据对象为键）
        /// </summary>
        private readonly Dictionary<object, bool> m_LiveFoldoutStates = new();

        /// <summary>
        /// 最近记录 POST 行的折叠状态缓存（以记录唯一键为键）
        /// </summary>
        private readonly Dictionary<string, bool> m_LogFoldoutStates = new();

        /// <summary>
        /// 上一帧统计数据（用于计算速率）
        /// </summary>
        private WebModuleDebugStat m_LastStat;

        #endregion

        #region 反射缓存

        /// <summary>
        /// WebModule 类型
        /// </summary>
        private Type m_ModuleType;

        /// <summary>
        /// WebModuleDebugInfo 类型
        /// </summary>
        private Type m_DebugInfoType;

        /// <summary>
        /// WebLiveRequestInfo 类型
        /// </summary>
        private Type m_LiveInfoType;

        /// <summary>
        /// WebLogEntry 类型
        /// </summary>
        private Type m_LogEntryType;

        /// <summary>
        /// WebJsonDataBase 类型（实时报文 Header/Form）
        /// </summary>
        private Type m_JsonDataBaseType;

        /// <summary>
        /// WebPbData 类型（实时报文字节数）
        /// </summary>
        private Type m_PbDataType;

        /// <summary>
        /// WebModule 实例
        /// </summary>
        private object m_ModuleInstance;

        /// <summary>
        /// WebModule.Instance 静态属性
        /// </summary>
        private PropertyInfo m_InstanceProperty;

        /// <summary>
        /// WebModule.Timeout 属性
        /// </summary>
        private PropertyInfo m_TimeoutProperty;

        /// <summary>
        /// WebModule.MaxConnectionPerServer 属性
        /// </summary>
        private PropertyInfo m_MaxConnProperty;

        /// <summary>
        /// WebModule.GetDebugSnapshot 方法
        /// </summary>
        private MethodInfo m_GetSnapshotMethod;

        /// <summary>
        /// WebModule.GetCurrentRequests 方法
        /// </summary>
        private MethodInfo m_GetCurrentRequestsMethod;

        /// <summary>
        /// WebModule.GetRecentLogs 方法
        /// </summary>
        private MethodInfo m_GetRecentLogsMethod;

        /// <summary>
        /// WebModule.ClearDebugHistory 方法
        /// </summary>
        private MethodInfo m_ClearDebugHistoryMethod;

        /// <summary>
        /// WebModule.CancelAllPendingForDebug 方法
        /// </summary>
        private MethodInfo m_CancelAllPendingMethod;

        /// <summary>
        /// WebModule.SendDebugRequest 方法
        /// </summary>
        private MethodInfo m_SendDebugRequestMethod;

        /// <summary>
        /// WebModule.ClearDebugManualResult 方法
        /// </summary>
        private MethodInfo m_ClearManualResultMethod;

        /// <summary>
        /// WebModule.SetDebugRecording 方法
        /// </summary>
        private MethodInfo m_SetDebugRecordingMethod;

        /// <summary>
        /// WebModule.DebugManualRunning 属性
        /// </summary>
        private PropertyInfo m_ManualRunningProperty;

        /// <summary>
        /// WebModule.DebugManualStatus 属性
        /// </summary>
        private PropertyInfo m_ManualStatusProperty;

        /// <summary>
        /// WebModule.DebugManualMessage 属性
        /// </summary>
        private PropertyInfo m_ManualMessageProperty;

        /// <summary>
        /// WebJsonDataBase.Header 属性
        /// </summary>
        private PropertyInfo m_JsonHeaderProperty;

        /// <summary>
        /// WebJsonDataBase.Form 属性
        /// </summary>
        private PropertyInfo m_JsonFormProperty;

        /// <summary>
        /// WebPbData.SendData 属性
        /// </summary>
        private PropertyInfo m_PbSendDataProperty;

        /// <summary>
        /// WebModuleDebugInfo 各属性缓存
        /// </summary>
        private Dictionary<string, PropertyInfo> m_DebugInfoProps;

        /// <summary>
        /// WebLiveRequestInfo 各属性缓存
        /// </summary>
        private Dictionary<string, PropertyInfo> m_LiveInfoProps;

        /// <summary>
        /// WebLogEntry 各属性缓存
        /// </summary>
        private Dictionary<string, PropertyInfo> m_LogEntryProps;

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
        /// 禁用：取消订阅 EditorApplication.update，并关闭模块调试记录开关
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            ApplyDebugRecording(false);
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

            // 非 Play 模式：先关闭调试记录开关再重置反射缓存，避免停止运行后持有已失效的热更实例
            if (!Application.isPlaying)
            {
                ApplyDebugRecording(false);
                ResetReflection();
                EditorGUILayout.HelpBox("需要在 Play 模式下使用", MessageType.Info);
                return;
            }

            if (!EnsureReflection())
            {
                EditorGUILayout.HelpBox("未能通过反射访问 WebModule，请确认 Hotfix 已加载", MessageType.Warning);
                return;
            }

            // 面板打开期间开启调试记录；关闭面板时 OnDisable 置回 false
            ApplyDebugRecording(true);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            var debugInfo = GetDebugSnapshot();
            if (debugInfo == null)
            {
                EditorGUILayout.HelpBox("未获取到 WebModule 调试快照", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawModuleOverview(debugInfo);
            DrawSectionDivider();

            DrawManualRequest();
            DrawSectionDivider();

            var liveInfos = GetAllLiveInfos();
            DrawLiveSection(liveInfos);
            DrawSectionDivider();

            var logInfos = GetAllLogInfos();
            DrawLogSection(logInfos);

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
            m_SearchFilter = GUILayout.TextField(m_SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));

            GUILayout.Space(10);
            m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.Space(10);
            GUILayout.Label("记录过滤:", GUILayout.Width(60));
            m_ResultFilters[0] = GUILayout.Toggle(m_ResultFilters[0], "成功", EditorStyles.toolbarButton, GUILayout.Width(48));
            m_ResultFilters[1] = GUILayout.Toggle(m_ResultFilters[1], "超时", EditorStyles.toolbarButton, GUILayout.Width(48));
            m_ResultFilters[2] = GUILayout.Toggle(m_ResultFilters[2], "失败", EditorStyles.toolbarButton, GUILayout.Width(48));
            m_ResultFilters[3] = GUILayout.Toggle(m_ResultFilters[3], "取消", EditorStyles.toolbarButton, GUILayout.Width(48));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 模块概览

        /// <summary>
        /// 绘制模块级概览：统计、速率、配置调整与模块级操作
        /// </summary>
        /// <param name="debugInfo">调试快照（装箱对象）</param>
        private void DrawModuleOverview(object debugInfo)
        {
            EditorGUILayout.LabelField("模块概览", EditorStyles.boldLabel);

            var submit     = GetIntProp(debugInfo, "SubmitCount");
            var jsonSubmit = GetIntProp(debugInfo, "JsonSubmitCount");
            var pbSubmit   = GetIntProp(debugInfo, "PbSubmitCount");
            var success    = GetIntProp(debugInfo, "SuccessCount");
            var failed     = GetIntProp(debugInfo, "FailedCount");
            var timeout    = GetIntProp(debugInfo, "TimeoutCount");
            var canceled   = GetIntProp(debugInfo, "CanceledCount");
            var sentBytes  = GetLongProp(debugInfo, "SentBytes");
            var recvBytes  = GetLongProp(debugInfo, "RecvBytes");

            var waitingJson = GetIntProp(debugInfo, "WaitingJsonCount");
            var sendingJson = GetIntProp(debugInfo, "SendingJsonCount");
            var waitingPb   = GetIntProp(debugInfo, "WaitingPbCount");
            var sendingPb   = GetIntProp(debugInfo, "SendingPbCount");
            var sendingAll  = sendingJson + sendingPb;

            var maxConn = m_MaxConnProperty?.GetValue(m_ModuleInstance) is int maxConnValue ? maxConnValue : 0;

            EditorGUILayout.LabelField(
                                       $"发起: {submit}（Json {jsonSubmit} / Pb {pbSubmit}）| 成功: {success} | 失败: {failed} | 超时: {timeout} | 取消: {canceled}");

            DrawRateLine(submit, sentBytes, recvBytes);

            EditorGUILayout.LabelField($"当前等待: {waitingJson + waitingPb}（Json {waitingJson} / Pb {waitingPb}）| 发送中: {sendingAll}（Json {sendingJson} / Pb {sendingPb}）| 并发占用: {sendingAll}/{maxConn}");

            // 配置调整
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("超时(秒):", GUILayout.Width(70));
            m_TimeoutText = GUILayout.TextField(m_TimeoutText, GUILayout.Width(60));
            GUILayout.Label("并发上限:", GUILayout.Width(70));
            m_MaxConnText = GUILayout.TextField(m_MaxConnText, GUILayout.Width(60));

            if (GUILayout.Button("应用", GUILayout.Width(60)))
            {
                ApplyConfig();
            }

            EditorGUILayout.EndHorizontal();

            // 模块级操作
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重置统计并清空记录", GUILayout.Width(160)))
            {
                TryInvoke(m_ClearDebugHistoryMethod, "清空记录失败");
            }

            var oldColor = GUI.color;
            GUI.color = Color.yellow;
            if (GUILayout.Button("清空等待并取消在途（急救）", GUILayout.Width(200)))
            {
                if (EditorUtility.DisplayDialog("Web模块急救", "将清空所有等待/发送中的请求并取消调用方任务，确定执行？", "确定", "取消"))
                {
                    TryInvoke(m_CancelAllPendingMethod, "急救操作失败");
                }
            }

            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制速率行：基于两次刷新统计差分计算每秒请求数、上行/下行字节速率
        /// </summary>
        /// <param name="submit">本次累计发起数</param>
        /// <param name="sentBytes">本次累计发送字节</param>
        /// <param name="recvBytes">本次累计接收字节</param>
        private void DrawRateLine(int submit, long sentBytes, long recvBytes)
        {
            var now = EditorApplication.timeSinceStartup;

            if (m_LastStat.SubmitCount >= 0 && now - m_LastStat.Time > 0.01)
            {
                var elapsed    = now - m_LastStat.Time;
                var reqPerSec  = (submit    - m_LastStat.SubmitCount) / elapsed;
                var upPerSec   = (sentBytes - m_LastStat.SentBytes)   / elapsed;
                var downPerSec = (recvBytes - m_LastStat.RecvBytes)   / elapsed;
                EditorGUILayout.LabelField($"速率: ≈{reqPerSec:0.0} req/s | ↑ {FormatBytes((long)upPerSec)}/s | ↓ {FormatBytes((long)downPerSec)}/s");
            }
            else
            {
                EditorGUILayout.LabelField($"收发总量: ↑ {FormatBytes(sentBytes)} | ↓ {FormatBytes(recvBytes)}");
            }

            m_LastStat = new WebModuleDebugStat(now, submit, sentBytes, recvBytes);
        }

        /// <summary>
        /// 应用超时与并发上限配置
        /// </summary>
        private void ApplyConfig()
        {
            if (m_ModuleInstance == null) return;

            if (float.TryParse(m_TimeoutText, out var timeout))
            {
                try
                {
                    m_TimeoutProperty?.SetValue(m_ModuleInstance, timeout);
                }
                catch (Exception e)
                {
                    Debug.LogError($"设置超时失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
                }
            }

            if (int.TryParse(m_MaxConnText, out var maxConn))
            {
                try
                {
                    m_MaxConnProperty?.SetValue(m_ModuleInstance, maxConn);
                }
                catch (Exception e)
                {
                    Debug.LogError($"设置并发上限失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
                }
            }
        }

        #endregion

        #region 手动测试请求

        /// <summary>
        /// 绘制手动测试请求区（URL 输入、GET/POST 切换、POST 请求体、响应结果展示）
        /// </summary>
        private void DrawManualRequest()
        {
            EditorGUILayout.LabelField("手动测试请求", EditorStyles.boldLabel);

            var running = ReadManualRunning();

            EditorGUILayout.BeginHorizontal();
            var methodNames = new[] { "GET", "POST" };
            m_ManualMethod = GUILayout.Toolbar(m_ManualMethod, methodNames, GUILayout.Width(120));
            m_ManualUrl    = GUILayout.TextField(m_ManualUrl, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            if (m_ManualMethod == 1)
            {
                GUILayout.Label("POST 请求体（JSON）:", EditorStyles.miniLabel);
                m_ManualBody = EditorGUILayout.TextArea(m_ManualBody, GUILayout.Height(60));
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !running && !string.IsNullOrEmpty(m_ManualUrl);
            if (GUILayout.Button("发送", GUILayout.Width(80)))
            {
                SendManualRequest();
            }

            GUI.enabled = !running;
            if (GUILayout.Button("清空", GUILayout.Width(60)))
            {
                ClearManualResult();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // 结果展示区：状态着色 + 响应/错误文本
            var status   = ReadManualStatus();
            var message  = ReadManualMessage();
            var color    = ManualStatusColor(status);
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Label(running ? "状态: 请求中（等待响应…）" : $"状态: {status}", EditorStyles.boldLabel);
            GUI.color = oldColor;

            if (!string.IsNullOrEmpty(message))
            {
                EditorGUILayout.SelectableLabel(message, GUILayout.MinHeight(48));
            }
        }

        /// <summary>
        /// 清空手动测试请求：模块侧状态/结果 + 本地的 URL / POST 请求体输入框
        /// </summary>
        private void ClearManualResult()
        {
            // 清空本地输入框
            m_ManualUrl  = "";
            m_ManualBody = "";

            if (m_ModuleInstance == null) return;

            try
            {
                m_ClearManualResultMethod?.Invoke(m_ModuleInstance, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"清空手动测试请求结果失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
            }
        }

        /// <summary>
        /// 触发模块侧手动测试请求
        /// </summary>
        private void SendManualRequest()
        {
            if (m_ModuleInstance == null) return;

            try
            {
                var url  = m_ManualUrl ?? "";
                var body = m_ManualMethod == 1 ? (m_ManualBody ?? "") : "";
                m_SendDebugRequestMethod?.Invoke(m_ModuleInstance, new object[] { url, m_ManualMethod == 1, body });
            }
            catch (Exception e)
            {
                Debug.LogError($"发送手动测试请求失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
            }
        }

        /// <summary>
        /// 读取模块 DebugManualRunning 属性
        /// </summary>
        /// <returns>是否正在请求</returns>
        private bool ReadManualRunning()
        {
            return m_ManualRunningProperty?.GetValue(m_ModuleInstance) is bool value && value;
        }

        /// <summary>
        /// 读取模块 DebugManualStatus 属性
        /// </summary>
        /// <returns>状态文本</returns>
        private string ReadManualStatus()
        {
            return m_ManualStatusProperty?.GetValue(m_ModuleInstance) as string ?? "空闲";
        }

        /// <summary>
        /// 读取模块 DebugManualMessage 属性
        /// </summary>
        /// <returns>结果文本</returns>
        private string ReadManualMessage()
        {
            return m_ManualMessageProperty?.GetValue(m_ModuleInstance) as string ?? "";
        }

        /// <summary>
        /// 将手动测试请求状态映射为展示颜色
        /// </summary>
        /// <param name="status">状态文本</param>
        /// <returns>颜色</returns>
        private static Color ManualStatusColor(string status)
        {
            switch (status)
            {
                case "成功":   return Color.green;
                case "失败":   return Color.red;
                case "参数错误": return Color.yellow;
                case "请求中":  return Color.yellow;
                case "已取消":  return Color.gray;
                default:     return Color.white;
            }
        }

        #endregion

        #region 实时请求

        /// <summary>
        /// 绘制实时请求区（JSON / Pb 两组）
        /// </summary>
        /// <param name="liveInfos">实时请求列表</param>
        private void DrawLiveSection(List<object> liveInfos)
        {
            EditorGUILayout.LabelField("实时请求", EditorStyles.boldLabel);

            if (liveInfos.Count == 0)
            {
                DrawGrayLabel("当前无等待或发送中的请求...");
                return;
            }

            // 已失效折叠状态清理：以请求数据对象为键，避免持有已完成的请求引用
            PruneFoldoutStates(liveInfos);

            foreach (var info in liveInfos)
            {
                DrawLiveRequest(info);
            }
        }

        /// <summary>
        /// 清理已不在实时列表中的折叠状态
        /// </summary>
        /// <param name="liveInfos">实时请求列表</param>
        private void PruneFoldoutStates(List<object> liveInfos)
        {
            if (m_LiveFoldoutStates.Count == 0) return;

            var alive = new HashSet<object>();
            foreach (var info in liveInfos)
            {
                var data = GetObjectProp(info, "Data");
                if (data != null) alive.Add(data);
            }

            if (alive.Count == 0)
            {
                m_LiveFoldoutStates.Clear();
                return;
            }

            var stale = new List<object>();
            foreach (var key in m_LiveFoldoutStates.Keys)
            {
                if (!alive.Contains(key)) stale.Add(key);
            }

            foreach (var key in stale)
            {
                m_LiveFoldoutStates.Remove(key);
            }
        }

        /// <summary>
        /// 绘制单个实时请求
        /// </summary>
        /// <param name="info">实时请求信息（装箱对象）</param>
        private void DrawLiveRequest(object info)
        {
            var data           = GetObjectProp(info, "Data");
            var state          = MapStateText(GetEnumPropValue(GetEnumProp(info, "State")));
            var isPb           = GetBoolProp(info, "IsPb");
            var isGet          = GetBoolProp(info, "IsGet");
            var url            = GetStringProp(info, "Url");
            var callerCanceled = GetBoolProp(info, "CallerCanceled");

            if (string.IsNullOrEmpty(url)) return;

            // 搜索过滤：URL 匹配才展示
            if (!string.IsNullOrEmpty(m_SearchFilter)
                && !url.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var protocol = isPb ? "Pb" : "Json";
            var method   = isGet ? "GET" : "POST";
            var duration = FormatLiveDuration(info);

            // 调用方已取消仍滞留的请求整行红色告警（取消链路泄漏）
            var oldColor                  = GUI.color;
            if (callerCanceled) GUI.color = Color.red;

            var foldoutText = $"[{state}] {protocol}·{method}  {url}  {duration}";
            var isOpen      = false;
            if (data != null)
            {
                m_LiveFoldoutStates.TryGetValue(data, out isOpen);
                isOpen                    = EditorGUILayout.Foldout(isOpen, foldoutText, true);
                m_LiveFoldoutStates[data] = isOpen;
            }
            else
            {
                GUILayout.Label(foldoutText);
            }

            GUI.color = oldColor;
            if (!isOpen) return;

            EditorGUILayout.BeginVertical("box");
            {
                if (callerCanceled)
                {
                    var warnColor = GUI.color;
                    GUI.color = Color.red;
                    GUILayout.Label("⚠ 调用方取消令牌已触发，但请求仍滞留，取消链路可能泄漏！");
                    GUI.color = warnColor;
                }

                DrawLivePayload(data, isPb);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        /// <summary>
        /// 格式化实时请求时长（等待/已发送）
        /// </summary>
        /// <param name="info">实时请求信息（装箱对象）</param>
        /// <returns>时长文本</returns>
        private string FormatLiveDuration(object info)
        {
            var state    = GetEnumPropValue(GetEnumProp(info, "State"));
            var enqueue  = GetDateTimeProp(info, "EnqueueTimeUtc");
            var sendTime = GetDateTimeProp(info, "SendTimeUtc");
            if (enqueue == default) return "";

            var nowUtc = DateTime.UtcNow;
            if (state == "Sending")
            {
                var send = sendTime == default ? enqueue : sendTime;
                return $"已发送 {FormatMs((int)(nowUtc - send).TotalMilliseconds)}";
            }

            return $"等待 {FormatMs((int)(nowUtc - enqueue).TotalMilliseconds)}";
        }

        /// <summary>
        /// 绘制实时请求报文内容
        /// </summary>
        /// <param name="data">请求数据对象</param>
        /// <param name="isPb">是否为 Pb 请求</param>
        private void DrawLivePayload(object data, bool isPb)
        {
            if (data == null)
            {
                DrawGrayLabel("无报文数据...");
                return;
            }

            if (isPb)
            {
                var sendData = m_PbSendDataProperty?.GetValue(data) as byte[];
                GUILayout.Label($"Pb 请求体: {sendData?.Length ?? 0} 字节（内容为二进制，不展开）");
                return;
            }

            var header = m_JsonHeaderProperty?.GetValue(data);
            var form   = m_JsonFormProperty?.GetValue(data);

            if (header is IDictionary headerDict && headerDict.Count > 0)
            {
                GUILayout.Label($"Header ({headerDict.Count}):");
                foreach (DictionaryEntry entry in headerDict)
                {
                    GUILayout.Label($"  {entry.Key}: {Truncate(entry.Value?.ToString(), 160)}", EditorStyles.miniLabel);
                }
            }

            if (form is IDictionary formDict && formDict.Count > 0)
            {
                GUILayout.Label($"Form ({formDict.Count}):");
                foreach (DictionaryEntry entry in formDict)
                {
                    GUILayout.Label($"  {entry.Key}: {Truncate(entry.Value?.ToString(), 160)}", EditorStyles.miniLabel);
                }
            }

            if ((header == null || ((IDictionary)header).Count == 0) && (form == null || ((IDictionary)form).Count == 0))
            {
                DrawGrayLabel("GET 请求，无请求体...");
            }
        }

        #endregion

        #region 最近请求记录

        /// <summary>
        /// 绘制最近请求记录区
        /// </summary>
        /// <param name="logInfos">记录列表</param>
        private void DrawLogSection(List<object> logInfos)
        {
            EditorGUILayout.LabelField($"最近请求记录（{logInfos.Count} 条）", EditorStyles.boldLabel);

            if (logInfos.Count == 0)
            {
                DrawGrayLabel("暂无记录...");
                return;
            }

            PruneLogFoldoutStates(logInfos);

            EditorGUILayout.BeginVertical("box");
            foreach (var logInfo in logInfos)
            {
                DrawLogEntry(logInfo);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 清理已不在当前记录列表中的 POST 折叠状态
        /// </summary>
        /// <param name="logInfos">记录列表</param>
        private void PruneLogFoldoutStates(List<object> logInfos)
        {
            if (m_LogFoldoutStates.Count == 0) return;

            var alive = new HashSet<string>();
            foreach (var logInfo in logInfos)
            {
                var key = BuildLogKey(logInfo);
                if (!string.IsNullOrEmpty(key)) alive.Add(key);
            }

            if (alive.Count == 0)
            {
                m_LogFoldoutStates.Clear();
                return;
            }

            var stale = new List<string>();
            foreach (var key in m_LogFoldoutStates.Keys)
            {
                if (!alive.Contains(key)) stale.Add(key);
            }

            foreach (var key in stale)
            {
                m_LogFoldoutStates.Remove(key);
            }
        }

        /// <summary>
        /// 生成最近记录行的折叠状态键（时刻 + 结果 + URL + 协议）
        /// </summary>
        /// <param name="logInfo">记录条目（装箱对象）</param>
        /// <returns>折叠状态键，无 URL 返回空串</returns>
        private string BuildLogKey(object logInfo)
        {
            var url = GetStringProp(logInfo, "Url");
            if (string.IsNullOrEmpty(url)) return "";

            var time     = GetDateTimeProp(logInfo, "CompleteTimeUtc");
            var result   = GetEnumPropValue(GetEnumProp(logInfo, "Result"));
            var isPb     = GetBoolProp(logInfo, "IsPb");
            var timePart = time == default ? "0" : time.Ticks.ToString();
            return $"{timePart}|{result}|{url}|{(isPb ? "Pb" : "Json")}";
        }

        /// <summary>
        /// 绘制单条最近请求记录
        /// </summary>
        /// <param name="logInfo">记录条目（装箱对象）</param>
        private void DrawLogEntry(object logInfo)
        {
            var result = GetEnumPropValue(GetEnumProp(logInfo, "Result"));
            if (string.IsNullOrEmpty(result)) return;

            // 结果类型过滤
            var resultIndex = ResultIndex(result);
            if (resultIndex < 0 || !m_ResultFilters[resultIndex]) return;

            var isPb      = GetBoolProp(logInfo, "IsPb");
            var isGet     = GetBoolProp(logInfo, "IsGet");
            var url       = GetStringProp(logInfo, "Url");
            var waitMs    = GetIntProp(logInfo, "WaitMs");
            var totalMs   = GetIntProp(logInfo, "TotalMs");
            var sendBytes = GetIntProp(logInfo, "SendBytes");
            var recvBytes = GetIntProp(logInfo, "RecvBytes");
            var error     = GetStringProp(logInfo, "Error");
            var time      = GetDateTimeProp(logInfo, "CompleteTimeUtc");

            if (string.IsNullOrEmpty(url)) return;

            // 搜索过滤：URL 匹配才展示
            if (!string.IsNullOrEmpty(m_SearchFilter)
                && !url.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var (label, color) = ResultStyle(result);
            var timeText = time == default ? "" : time.ToLocalTime().ToString("HH:mm:ss.fff");
            var protocol = isPb ? "Pb" : "Json";
            var method   = isGet ? "GET" : "POST";
            var isPost   = !isGet;

            var errorText = string.IsNullOrEmpty(error) ? "" : $"  错误: {Truncate(error, 120)}";
            var lineText  = $"{timeText}  [{label}] {protocol}·{method}  等待 {FormatMs(waitMs)}/总 {FormatMs(totalMs)}  ↑{FormatBytes(sendBytes)} ↓{FormatBytes(recvBytes)}  {url}{errorText}";

            var oldColor = GUI.color;
            EditorGUILayout.BeginHorizontal();

            // POST 记录：前置展开/折叠箭头按钮，展开可查看/复制请求体；GET 记录无折叠
            var isOpen = false;
            if (isPost)
            {
                var key = BuildLogKey(logInfo);
                m_LogFoldoutStates.TryGetValue(key, out isOpen);
                var arrow = isOpen ? "▼" : "▶";
                if (GUILayout.Button(arrow, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    isOpen                  = !isOpen;
                    m_LogFoldoutStates[key] = isOpen;
                }
            }

            GUI.color = color;
            GUILayout.Label(lineText, GUILayout.ExpandWidth(true));
            GUI.color = oldColor;

            if (GUILayout.Button("复制URL", GUILayout.Width(72)))
            {
                EditorGUIUtility.systemCopyBuffer = url;
            }

            EditorGUILayout.EndHorizontal();

            // 展开的 POST 记录：展示请求体并支持复制
            if (isPost && isOpen)
            {
                DrawLogRequestBody(logInfo, isPb, sendBytes);
            }
        }

        /// <summary>
        /// 绘制展开后的 POST 记录请求体（含复制按钮）
        /// </summary>
        /// <param name="logInfo">记录条目（装箱对象）</param>
        /// <param name="isPb">是否为 Pb 请求</param>
        /// <param name="sendBytes">发送字节数</param>
        private void DrawLogRequestBody(object logInfo, bool isPb, int sendBytes)
        {
            EditorGUILayout.BeginVertical("box");

            var body = GetStringProp(logInfo, "RequestBody");
            if (string.IsNullOrEmpty(body))
            {
                DrawGrayLabel(isPb
                                  ? $"Pb 请求体为二进制，共 {FormatBytes(sendBytes)} 字节，未能生成类 JSON 预览。"
                                  : "POST 无请求体（空表单）。");
            }
            else
            {
                GUILayout.Label(isPb ? "请求体（Pb 类 JSON 预览）:" : "请求体:");
                // 运行时存紧凑文本，展示时若仍为单行紧凑 JSON 则缩进美化（已美化的直接显示），避免运行期序列化开销
                var displayBody   = body.IndexOf('\n') >= 0 ? body : IndentJson(body);
                var wrapStyle     = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                var previewHeight = CalcContentHeight(displayBody, wrapStyle);
                EditorGUILayout.SelectableLabel(displayBody, wrapStyle, GUILayout.Height(previewHeight));

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("复制请求体", GUILayout.Width(90)))
                {
                    EditorGUIUtility.systemCopyBuffer = displayBody;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
        }

        /// <summary>
        /// 按文本内容与换行样式计算预览框自适应高度（含最小高度）
        /// </summary>
        /// <param name="text">预览文本</param>
        /// <param name="style">换行样式</param>
        /// <returns>预览框高度</returns>
        private float CalcContentHeight(string text, GUIStyle style)
        {
            const float minHeight     = 40f;
            const float widthPadding  = 40f; // 估算盒内可用宽度扣除的内外边距与滚动条宽度
            var         contentWidth  = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - widthPadding);
            var         contentHeight = style.CalcHeight(new GUIContent(text), contentWidth);
            return Mathf.Max(contentHeight, minHeight);
        }

        /// <summary>
        /// 将结果枚举名映射为过滤索引（0 成功 / 1 超时 / 2 失败 / 3 取消），未知返回 -1
        /// </summary>
        /// <param name="result">结果枚举名</param>
        /// <returns>过滤索引</returns>
        private static int ResultIndex(string result)
        {
            switch (result)
            {
                case "Success":  return 0;
                case "Timeout":  return 1;
                case "Failed":   return 2;
                case "Canceled": return 3;
                default:         return -1;
            }
        }

        /// <summary>
        /// 返回结果的中文标签与展示颜色
        /// </summary>
        /// <param name="result">结果枚举名</param>
        /// <returns>标签与颜色</returns>
        private static (string, Color) ResultStyle(string result)
        {
            switch (result)
            {
                case "Success":  return ("成功", Color.green);
                case "Timeout":  return ("超时", Color.yellow);
                case "Failed":   return ("失败", Color.red);
                case "Canceled": return ("取消", Color.gray);
                default:         return (result, Color.white);
            }
        }

        #endregion

        #region 反射取值辅助

        /// <summary>
        /// 取 WebModuleDebugInfo/WebLiveRequestInfo 布尔属性
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>布尔值</returns>
        private bool GetBoolProp(object target, string name)
        {
            return GetPropValue(target, name) is bool value && value;
        }

        /// <summary>
        /// 取 WebModuleDebugInfo/WebLogEntry 整型属性
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>整型值</returns>
        private int GetIntProp(object target, string name)
        {
            return GetPropValue(target, name) is int value ? value : 0;
        }

        /// <summary>
        /// 取长整型属性
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>长整型值</returns>
        private long GetLongProp(object target, string name)
        {
            return GetPropValue(target, name) is long value ? value : 0L;
        }

        /// <summary>
        /// 取字符串属性
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>字符串值</returns>
        private string GetStringProp(object target, string name)
        {
            return GetPropValue(target, name) as string;
        }

        /// <summary>
        /// 取 DateTime 属性
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>日期时间值</returns>
        private DateTime GetDateTimeProp(object target, string name)
        {
            return GetPropValue(target, name) is DateTime value ? value : default;
        }

        /// <summary>
        /// 取对象属性（如 Data）
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>对象值</returns>
        private static object GetObjectProp(object target, string name)
        {
            return target?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target);
        }

        /// <summary>
        /// 取枚举属性（装箱值）
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>枚举装箱值</returns>
        private static object GetEnumProp(object target, string name)
        {
            return target?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target);
        }

        /// <summary>
        /// 取属性值（使用缓存属性）
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <param name="name">属性名</param>
        /// <returns>属性值</returns>
        private object GetPropValue(object target, string name)
        {
            var props = SelectPropCache(target);
            if (props == null) return null;

            return props.TryGetValue(name, out var prop) ? prop.GetValue(target) : null;
        }

        /// <summary>
        /// 根据装箱对象的运行时类型选择对应属性缓存
        /// </summary>
        /// <param name="target">装箱对象</param>
        /// <returns>属性名到属性的缓存</returns>
        private Dictionary<string, PropertyInfo> SelectPropCache(object target)
        {
            if (target == null) return null;
            if (m_DebugInfoType != null && m_DebugInfoType.IsInstanceOfType(target)) return m_DebugInfoProps;
            if (m_LiveInfoType  != null && m_LiveInfoType.IsInstanceOfType(target)) return m_LiveInfoProps;
            if (m_LogEntryType  != null && m_LogEntryType.IsInstanceOfType(target)) return m_LogEntryProps;
            return null;
        }

        /// <summary>
        /// 将枚举装箱值转为枚举名字符串
        /// </summary>
        /// <param name="enumValue">枚举装箱值</param>
        /// <returns>枚举名字符串</returns>
        private static string GetEnumPropValue(object enumValue)
        {
            return enumValue?.ToString() ?? "";
        }

        /// <summary>
        /// 将请求状态枚举名映射为中文状态文本
        /// </summary>
        /// <param name="stateName">状态枚举名</param>
        /// <returns>中文状态文本</returns>
        private static string MapStateText(string stateName)
        {
            if (stateName == "Waiting") return "等待";
            if (stateName == "Sending") return "发送中";
            return stateName;
        }

        #endregion

        #region 反射查询与调用

        /// <summary>
        /// 确保反射缓存已初始化
        /// </summary>
        /// <returns>初始化成功返回 true</returns>
        private bool EnsureReflection()
        {
            if (m_ModuleInstance != null) return true;

            m_ModuleType = Type.GetType("Hotfix.Framework.Web.WebModule, Hotfix");
            if (m_ModuleType == null) return false;

            m_DebugInfoType = Type.GetType("Hotfix.Framework.Web.WebModuleDebugInfo, Hotfix");
            if (m_DebugInfoType == null) return false;

            m_LiveInfoType = Type.GetType("Hotfix.Framework.Web.WebLiveRequestInfo, Hotfix");
            if (m_LiveInfoType == null) return false;

            m_LogEntryType = Type.GetType("Hotfix.Framework.Web.WebLogEntry, Hotfix");
            if (m_LogEntryType == null) return false;

            m_JsonDataBaseType = Type.GetType("Hotfix.Framework.Web.WebJsonDataBase, Hotfix");
            m_PbDataType       = Type.GetType("Hotfix.Framework.Web.WebPbData, Hotfix");

            // WebModule 单例通过静态 Instance 属性获取（OnInit 时登记）
            m_InstanceProperty = m_ModuleType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (m_InstanceProperty == null) return false;

            m_ModuleInstance = m_InstanceProperty.GetValue(null);
            if (m_ModuleInstance == null) return false;

            // WebModule 成员
            m_TimeoutProperty          = m_ModuleType.GetProperty("Timeout",                BindingFlags.Public | BindingFlags.Instance);
            m_MaxConnProperty          = m_ModuleType.GetProperty("MaxConnectionPerServer", BindingFlags.Public | BindingFlags.Instance);
            m_GetSnapshotMethod        = m_ModuleType.GetMethod("GetDebugSnapshot",         BindingFlags.Public | BindingFlags.Instance);
            m_GetCurrentRequestsMethod = m_ModuleType.GetMethod("GetCurrentRequests",       BindingFlags.Public | BindingFlags.Instance);
            m_GetRecentLogsMethod      = m_ModuleType.GetMethod("GetRecentLogs",            BindingFlags.Public | BindingFlags.Instance);
            m_ClearDebugHistoryMethod  = m_ModuleType.GetMethod("ClearDebugHistory",        BindingFlags.Public | BindingFlags.Instance);
            m_CancelAllPendingMethod   = m_ModuleType.GetMethod("CancelAllPendingForDebug", BindingFlags.Public | BindingFlags.Instance);
            m_SendDebugRequestMethod   = m_ModuleType.GetMethod("SendDebugRequest",         BindingFlags.Public | BindingFlags.Instance);
            m_ClearManualResultMethod  = m_ModuleType.GetMethod("ClearDebugManualResult",   BindingFlags.Public | BindingFlags.Instance);
            m_SetDebugRecordingMethod  = m_ModuleType.GetMethod("SetDebugRecording",        BindingFlags.Public | BindingFlags.Instance);
            m_ManualRunningProperty    = m_ModuleType.GetProperty("DebugManualRunning", BindingFlags.Public     | BindingFlags.Instance);
            m_ManualStatusProperty     = m_ModuleType.GetProperty("DebugManualStatus",  BindingFlags.Public     | BindingFlags.Instance);
            m_ManualMessageProperty    = m_ModuleType.GetProperty("DebugManualMessage", BindingFlags.Public     | BindingFlags.Instance);

            // 结构体属性缓存
            m_DebugInfoProps = BuildPropCache(m_DebugInfoType);
            m_LiveInfoProps  = BuildPropCache(m_LiveInfoType);
            m_LogEntryProps  = BuildPropCache(m_LogEntryType);

            // 报文属性（可能为 null，扩展失败则无法展开报文）
            m_JsonHeaderProperty = m_JsonDataBaseType?.GetProperty("Header", BindingFlags.Public | BindingFlags.Instance);
            m_JsonFormProperty   = m_JsonDataBaseType?.GetProperty("Form",   BindingFlags.Public | BindingFlags.Instance);
            m_PbSendDataProperty = m_PbDataType?.GetProperty("SendData", BindingFlags.Public     | BindingFlags.Instance);

            return true;
        }

        /// <summary>
        /// 构建类型的公共实例属性缓存
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>属性名到属性的缓存</returns>
        private static Dictionary<string, PropertyInfo> BuildPropCache(Type type)
        {
            var props = new Dictionary<string, PropertyInfo>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                props[prop.Name] = prop;
            }

            return props;
        }

        /// <summary>
        /// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
        /// </summary>
        private void ResetReflection()
        {
            m_ModuleType               = null;
            m_DebugInfoType            = null;
            m_LiveInfoType             = null;
            m_LogEntryType             = null;
            m_JsonDataBaseType         = null;
            m_PbDataType               = null;
            m_ModuleInstance           = null;
            m_InstanceProperty         = null;
            m_TimeoutProperty          = null;
            m_MaxConnProperty          = null;
            m_GetSnapshotMethod        = null;
            m_GetCurrentRequestsMethod = null;
            m_GetRecentLogsMethod      = null;
            m_ClearDebugHistoryMethod  = null;
            m_CancelAllPendingMethod   = null;
            m_SendDebugRequestMethod   = null;
            m_ClearManualResultMethod  = null;
            m_SetDebugRecordingMethod  = null;
            m_ManualRunningProperty    = null;
            m_ManualStatusProperty     = null;
            m_ManualMessageProperty    = null;
            m_JsonHeaderProperty       = null;
            m_JsonFormProperty         = null;
            m_PbSendDataProperty       = null;
            m_DebugInfoProps           = null;
            m_LiveInfoProps            = null;
            m_LogEntryProps            = null;
            m_LastStat                 = default;
            m_LiveFoldoutStates.Clear();
            m_LogFoldoutStates.Clear();
        }

        /// <summary>
        /// 获取调试快照（装箱对象）
        /// </summary>
        /// <returns>调试快照对象，失败返回 null</returns>
        private object GetDebugSnapshot()
        {
            return m_GetSnapshotMethod?.Invoke(m_ModuleInstance, null);
        }

        /// <summary>
        /// 获取全部实时请求（装箱列表）
        /// </summary>
        /// <returns>实时请求列表</returns>
        private List<object> GetAllLiveInfos()
        {
            var list   = new List<object>();
            var result = m_GetCurrentRequestsMethod?.Invoke(m_ModuleInstance, null) as IEnumerable;
            if (result == null) return list;

            foreach (var item in result)
            {
                if (item != null) list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// 获取全部最近请求记录（装箱列表）
        /// </summary>
        /// <returns>记录列表</returns>
        private List<object> GetAllLogInfos()
        {
            var list   = new List<object>();
            var result = m_GetRecentLogsMethod?.Invoke(m_ModuleInstance, null) as IEnumerable;
            if (result == null) return list;

            foreach (var item in result)
            {
                if (item != null) list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// 安全调用返回 void 的方法并捕获异常
        /// </summary>
        /// <param name="method">方法</param>
        /// <param name="failMessage">失败日志前缀</param>
        private void TryInvoke(MethodInfo method, string failMessage)
        {
            if (method == null || m_ModuleInstance == null) return;

            try
            {
                method.Invoke(m_ModuleInstance, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"{failMessage}，异常为“{e.InnerException?.Message ?? e.Message}”.");
            }
        }

        /// <summary>
        /// 应用模块调试记录开关（面板打开 true / 关闭 false），停止运行瞬间的失效调用静默忽略
        /// </summary>
        /// <param name="enabled">是否开启</param>
        private void ApplyDebugRecording(bool enabled)
        {
            if (m_ModuleInstance == null || m_SetDebugRecordingMethod == null) return;

            try
            {
                m_SetDebugRecordingMethod.Invoke(m_ModuleInstance, new object[] { enabled });
            }
            catch
            {
                // 忽略：Play 停止/域重载瞬间模块实例可能已失效
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 绘制区块间的明显横向分割线（带上下留白）
        /// </summary>
        private static void DrawSectionDivider()
        {
            EditorGUILayout.Space();

            var lineRect = GUILayoutUtility.GetRect(0f, 2f);
            lineRect.x     += 4f;
            lineRect.width =  Mathf.Max(1f, lineRect.width - 8f);
            EditorGUI.DrawRect(lineRect, new Color(0.35f, 0.35f, 0.35f, 0.85f));

            EditorGUILayout.Space();
        }

        /// <summary>
        /// 将紧凑 JSON 文本格式化为带缩进的多行文本（供展示，不做合法性校验）
        /// </summary>
        /// <param name="json">紧凑 JSON 文本</param>
        /// <returns>缩进美化后的文本</returns>
        private static string IndentJson(string json)
        {
            var sb     = new StringBuilder(json.Length + 64);
            var indent = 0;
            var inString = false;

            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];

                if (inString)
                {
                    sb.Append(c);
                    if (c == '\\' && i + 1 < json.Length)
                    {
                        sb.Append(json[++i]);
                        continue;
                    }

                    if (c == '"') inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        AppendJsonIndent(sb, indent);
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent = Mathf.Max(0, indent - 1);
                        AppendJsonIndent(sb, indent);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        AppendJsonIndent(sb, indent);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 向 StringBuilder 追加指定层数的缩进
        /// </summary>
        /// <param name="sb">构建器</param>
        /// <param name="indent">缩进层数</param>
        private static void AppendJsonIndent(StringBuilder sb, int indent)
        {
            for (var j = 0; j < indent; j++)
            {
                sb.Append("    ");
            }
        }

        /// <summary>
        /// 绘制灰色提示标签
        /// </summary>
        /// <param name="text">文本</param>
        private static void DrawGrayLabel(string text)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(text, EditorStyles.miniLabel);
            GUI.color = oldColor;
        }

        /// <summary>
        /// 格式化毫秒为人类可读文本
        /// </summary>
        /// <param name="ms">毫秒</param>
        /// <returns>格式化文本</returns>
        private static string FormatMs(int ms)
        {
            if (ms < 0) return "0ms";
            if (ms < 1000) return $"{ms}ms";
            return $"{ms / 1000f:0.00}s";
        }

        /// <summary>
        /// 格式化字节数
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化文本</returns>
        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes}B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:0.0}KB";
            return $"{bytes / (1024f * 1024f):0.0}MB";
        }

        /// <summary>
        /// 截断过长文本
        /// </summary>
        /// <param name="text">原文本</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>截断后的文本</returns>
        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "...";
        }

        #endregion

        #region 嵌套类型

        /// <summary>
        /// 用于速率计算的上一帧统计快照
        /// </summary>
        private readonly struct WebModuleDebugStat
        {
            /// <summary>
            /// 采样时间（EditorApplication.timeSinceStartup）
            /// </summary>
            public double Time { get; }

            /// <summary>
            /// 上一帧累计发起数
            /// </summary>
            public int SubmitCount { get; }

            /// <summary>
            /// 上一帧累计发送字节
            /// </summary>
            public long SentBytes { get; }

            /// <summary>
            /// 上一帧累计接收字节
            /// </summary>
            public long RecvBytes { get; }

            /// <summary>
            /// 初始化统计快照
            /// </summary>
            /// <param name="time">采样时间</param>
            /// <param name="submitCount">累计发起数</param>
            /// <param name="sentBytes">累计发送字节</param>
            /// <param name="recvBytes">累计接收字节</param>
            public WebModuleDebugStat(double time, int submitCount, long sentBytes, long recvBytes)
            {
                Time        = time;
                SubmitCount = submitCount;
                SentBytes   = sentBytes;
                RecvBytes   = recvBytes;
            }
        }

        #endregion
    }
}
#endif