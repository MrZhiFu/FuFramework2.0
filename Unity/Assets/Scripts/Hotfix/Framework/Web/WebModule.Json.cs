using System;
using System.Text;
using System.Threading;
using UnityEngine.Networking;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 管理模块的 JSON 请求处理。
    /// 功能：
    ///     1. 处理 JSON 请求的出队、构建、发送与完成回调写回。
    ///     2. 负责清空 JSON 请求队列与列表。
    /// </summary>
    public partial class WebModule
    {
        /// <summary>
        /// 更新处理 JSON 请求队列。
        /// </summary>
        private void UpdateJsonReq()
        {
            // 每帧填满全部空闲并发槽位（而非每帧仅发一个），提升吞吐
            while (m_SendingJsonList.Count < MaxConnectionPerServer && m_WaitingJsonQueue.Count > 0)
            {
                var webJsonData = m_WaitingJsonQueue.Dequeue();
                // 构造失败（非法 URL/Header 等）已回写异常并返回 false，不入发送列表
                if (SendJsonReq(webJsonData))
                    m_SendingJsonList.Add(webJsonData);
            }
        }

        /// <summary>
        /// 清空 JSON 请求队列与列表，取消未完成的任务。
        /// </summary>
        private void ClearJsonReq()
        {
            while (m_WaitingJsonQueue.Count > 0)
            {
                var webData = m_WaitingJsonQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingJsonQueue.Clear();

            while (m_SendingJsonList.Count > 0)
            {
                var webData = m_SendingJsonList[0];
                m_SendingJsonList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingJsonList.Clear();
        }

        /// <summary>
        /// 构建并发送 JSON 请求（GET/POST），完成回调按子类结果类型（字符串或字节数组）写回。
        /// </summary>
        /// <param name="webJsonData">JSON 请求数据（WebJsonStringData 或 WebJsonBytesData）。</param>
        /// <returns>是否成功发起并登记在途；构建失败返回 false（已回写异常，调用方不会挂起）。</returns>
        private bool SendJsonReq(WebJsonDataBase webJsonData)
        {
            FuLogger.LogInfo($"Web Request: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");

            // 前置构建 + 发送请求：非法 URL/Header 或发送异常在此抛，回写异常并返回，不登记在途、不注册回调
            UnityWebRequest unityWebRequest = null;
            UnityWebRequestAsyncOperation asyncOperation = null;
            try
            {
                unityWebRequest = BuildJsonRequest(webJsonData);
                asyncOperation  = unityWebRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                // 构建/发送失败：释放原生资源并回写异常，避免调用方永久挂起
                unityWebRequest?.Dispose();
                webJsonData.CompleteError(e);
                return false;
            }

            // 构建 + 发送成功后才登记在途：失败路径无在途登记，计数不泄漏
            var capturedToken = m_Scope.Token;     // 发起时捕获生命周期 Token：模块销毁/重启（OnDispose Cancel）后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var sendingList   = m_SendingJsonList; // 捕获发送列表引用，完成回调不捕获 this（模块实例）
            var inFlight      = m_Scope.Begin();   // 登记在途：CancelAsync 等待本请求清理完毕

            // 任一 token 取消即 Abort 中断传输，避免在途请求等到超时才被回收；完成回调中注销。
            // 模块 scope token 恒可取消；仅在存在可取消 token 时创建链接源，减少无谓分配。
            CancellationTokenSource linkedCts         = null;
            var                     abortRegistration = default(CancellationTokenRegistration);
            if (capturedToken.CanBeCanceled || webJsonData.Token.CanBeCanceled)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(capturedToken, webJsonData.Token);
                abortRegistration = linkedCts.Token.Register(() =>
                {
                    try
                    {
                        unityWebRequest.Abort();
                    }
                    catch
                    {
                        // 请求可能已释放，忽略
                    }
                });
            }

            // 闭包捕获请求引用副本：外层 catch（构建/发送失败路径）释放的是原始变量，
            // 闭包仅在请求完成时运行（副本持有有效请求），两者运行时互斥，消除"捕获变量在外部释放"告警
            var requestRef = unityWebRequest;

            asyncOperation.completed += _ =>
            {
                try
                {
                    // 无论成功/失败/取消，先释放并发槽位：否则调用方取消会永久占用槽位，最终阻塞全部新请求
                    sendingList.Remove(webJsonData);

                    // 模块销毁/重启（旧 scope 于 OnDispose Cancel，capturedToken 已触发）或调用方取消：按取消处理，不再写回结果。
                    // 注：重启时序 DisposeModules（OnDispose→Cancel）恒先于重新初始化，故无需再比对实时 m_Scope.Token。
                    if (capturedToken.IsCancellationRequested || webJsonData.Token.IsCancellationRequested)
                    {
                        webJsonData.CompleteCanceled();
                        return;
                    }

                    if (requestRef.result != UnityWebRequest.Result.Success)
                    {
                        FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)} \n Content: {requestRef.error}");

                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsTimeout(requestRef))
                            webJsonData.CompleteError(new TimeoutException(requestRef.error));
                        else
                            webJsonData.CompleteError(new Exception(requestRef.error));
                        return;
                    }

                    FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");
                    webJsonData.Complete(requestRef);
                }
                finally
                {
                    abortRegistration.Dispose(); // 注销取消回调，释放对请求的引用
                    linkedCts?.Dispose();        // 释放链接 CTS（可能未创建）
                    requestRef.Dispose();        // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                    inFlight.Dispose();          // 结束在途登记，CancelAsync 据此等待清理完毕
                }
            };
            return true;
        }

        /// <summary>
        /// 构建 JSON 请求（GET/POST + 超时 + 表单/请求头）。
        /// 构建失败时释放已创建资源并抛出原异常，由调用方回写异常。
        /// </summary>
        /// <param name="webJsonData">JSON 请求数据。</param>
        /// <returns>构建完成的请求。</returns>
        private UnityWebRequest BuildJsonRequest(WebJsonDataBase webJsonData)
        {
            // GET 用 UnityWebRequest.Get；POST 手动构造（避免 PostWwwForm 先生成空 WWWForm 再被替换的重复分配）
            var unityWebRequest = webJsonData.IsGet
                ? UnityWebRequest.Get(webJsonData.URL)
                : new UnityWebRequest(webJsonData.URL, UnityWebRequest.kHttpVerbPOST)
                {
                    downloadHandler = new DownloadHandlerBuffer(),
                };

            try
            {
                unityWebRequest.timeout = (int)ReqTimeout.TotalSeconds;
                if (webJsonData.Form is { Count: > 0 })
                {
                    unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                    var body     = UtilityAOT.Json.ToJson(webJsonData.Form);
                    var postData = Encoding.UTF8.GetBytes(body);
                    unityWebRequest.uploadHandler = new UploadHandlerRaw(postData);
                }
                else if (!webJsonData.IsGet)
                {
                    // 空表单 POST：保留 form-urlencoded 内容类型契约（与旧 PostWwwForm 一致），body 为空
                    unityWebRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                }

                if (webJsonData.Header is { Count: > 0 })
                {
                    foreach (var kv in webJsonData.Header)
                    {
                        unityWebRequest.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                return unityWebRequest;
            }
            catch
            {
                // 构建失败：释放原生资源后抛出
                unityWebRequest.Dispose();
                throw;
            }
        }
    }
}