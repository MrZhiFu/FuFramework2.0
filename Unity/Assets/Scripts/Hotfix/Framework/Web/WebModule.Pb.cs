using System;
using System.Threading;
using ProtoBuf;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Hotfix.Framework.Network;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 管理模块的 Pb 请求处理。
    /// 功能：
    ///     1. 出队 Pb 请求并构建发送，完成回调经 SendRequest 共享骨架写回字节结果。
    ///     2. 完成写回协议由 WebPbData 实现。
    /// </summary>
    public partial class WebModule
    {
        /// <summary>
        /// 更新处理 Pb 请求队列。
        /// </summary>
        private void UpdatePbReq()
        {
            // 每帧填满全部空闲并发槽位（而非每帧仅发一个），提升吞吐
            while (m_SendingPbList.Count < MaxConnectionPerServer && m_WaitingPbQueue.Count > 0)
            {
                var webPbData = m_WaitingPbQueue.Dequeue();
                if (SendPbReq(webPbData))
                    m_SendingPbList.Add(webPbData);
            }
        }

        /// <summary>
        /// 构建并发送 Pb 请求，完成后经 SendRequest 共享骨架写回字节结果。
        /// </summary>
        /// <param name="webData">Pb 请求数据。</param>
        /// <returns>是否成功发起并登记在途；构建失败返回 false（已回写异常，调用方不会挂起）。</returns>
        private bool SendPbReq(WebPbData webData)
        {
            // 前置构建 + 发送请求：非法 URL/Header 或发送异常在此抛，回写异常并返回，不登记在途、不注册回调
            UnityWebRequest unityWebRequest = null;
            UnityWebRequestAsyncOperation asyncOperation = null;
            try
            {
                unityWebRequest = BuildPbRequest(webData);
                asyncOperation  = unityWebRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                // 构建/发送失败：释放原生资源并回写异常，避免调用方永久挂起
                unityWebRequest?.Dispose();
                webData.CompleteError(e);
                return false;
            }

            // 构建 + 发送成功后才登记在途：失败路径无在途登记，计数不泄漏
            return SendRequest(webData, m_SendingPbList, unityWebRequest, asyncOperation);
        }

        /// <summary>
        /// 构建 Pb 请求（POST + 超时 + Pb 内容类型 + 请求体）。
        /// 构建失败时释放已创建资源并抛出原异常，由调用方回写异常。
        /// </summary>
        /// <param name="webData">Pb 请求数据。</param>
        /// <returns>构建完成的请求。</returns>
        private UnityWebRequest BuildPbRequest(WebPbData webData)
        {
            // Pb 请求一律 POST；手动构造避免 PostWwwForm 先生成空 WWWForm 的重复分配
            var unityWebRequest = new UnityWebRequest(webData.URL, UnityWebRequest.kHttpVerbPOST)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };

            try
            {
                unityWebRequest.timeout = (int)ReqTimeout.TotalSeconds;
                unityWebRequest.SetRequestHeader("Content-Type", PbContentType);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(webData.SendData);
                return unityWebRequest;
            }
            catch
            {
                // 构建失败：释放原生资源后抛出
                unityWebRequest.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Pb POST 请求入队（公共 API 核心）。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="message">消息对象。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>返回 WebBufferResult 类型的异步任务。</returns>
        private UniTask<WebBufferResult> PostPbReq(string url, MessageObject message, CancellationToken token, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
            token.ThrowIfCancellationRequested();         // 调用方已取消则拒绝新请求
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = NormalizeURL(url, null);
            var id = ProtoMessageIdHandler.GetReqMessageIdByType(message.GetType());
            var messageHttpObject = new MessageHttpObject
            {
                Id       = id,
                UniqueId = message.UniqueId,
                Body     = SerializerHelper.Serialize(message),
            };
            var sendData = SerializerHelper.Serialize(messageHttpObject);
            var webData  = new WebPbData(url, sendData, uniTaskCompletionSource, token, userData);
            m_WaitingPbQueue.Enqueue(webData);
            return uniTaskCompletionSource.Task;
        }
    }
}