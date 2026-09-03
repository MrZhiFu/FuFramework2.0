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
    ///     1. 出队 JSON 请求并构建发送，完成回调经 SendRequest 共享骨架按子类结果类型写回。
    ///     2. 完成写回协议由子类（WebJsonStringData/WebJsonBytesData）实现。
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
                if (SendJsonReq(webJsonData))
                    m_SendingJsonList.Add(webJsonData);
            }
        }

        /// <summary>
        /// 构建并发送 JSON 请求（GET/POST），完成后经 SendRequest 共享骨架按子类结果类型写回。
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
            return SendRequest(webJsonData, m_SendingJsonList, unityWebRequest, asyncOperation);
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