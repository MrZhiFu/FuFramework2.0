using System;
using ProtoBuf;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using System.Collections.Generic;
using Hotfix.Framework.Network;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web管理模块的ProtoBuf部分实现
    /// </summary>
    public partial class WebModule
    {
        /// <summary>
        /// 等待处理的ProtoBuf请求队列
        /// </summary>
        private readonly Queue<WebProtoBufData> m_WaitingProtoBufQueue = new(256);

        /// <summary>
        /// 正在处理的ProtoBuf请求列表
        /// </summary>
        private readonly List<WebProtoBufData> m_SendingProtoBufList = new(16);

        /// <summary>
        /// ProtoBuf内容类型常量
        /// </summary>
        private const string ProtoBufContentType = "application/x-protobuf";

        /// <summary>
        /// 更新处理ProtoBuf请求队列
        /// </summary>
        private void UpdateProtoBuf()
        {
            lock (m_UrlStr)
            {
                if (m_SendingProtoBufList.Count >= MaxConnectionPerServer || m_WaitingProtoBufQueue.Count <= 0) return;
                var webProtoBufData = m_WaitingProtoBufQueue.Dequeue();
                MakeProtoBufBytesRequest(webProtoBufData);
                m_SendingProtoBufList.Add(webProtoBufData);
            }
        }

        /// <summary>
        /// 关闭ProtoBuf请求处理，清理资源
        /// </summary>
        private void ShutdownProtoBuf()
        {
            while (m_WaitingProtoBufQueue.Count > 0)
            {
                var webData = m_WaitingProtoBufQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingProtoBufQueue.Clear();

            while (m_SendingProtoBufList.Count > 0)
            {
                var webData = m_SendingProtoBufList[0];
                m_SendingProtoBufList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingProtoBufList.Clear();
        }

        /// <summary>
        /// 执行ProtoBuf字节请求
        /// </summary>
        /// <param name="webData">ProtoBuf请求数据</param>
        private void MakeProtoBufBytesRequest(WebProtoBufData webData)
        {
            var capturedToken = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var unityWebRequest = webData.IsGet ? UnityWebRequest.Get(webData.URL) : UnityWebRequest.PostWwwForm(webData.URL, string.Empty);

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            unityWebRequest.SetRequestHeader("Content-Type", ProtoBufContentType);
            unityWebRequest.uploadHandler = new UploadHandlerRaw(webData.SendData);

            var asyncOperation = unityWebRequest.SendWebRequest();
            asyncOperation.completed += _ =>
            {
                try
                {
                    // 模块已销毁/生命周期变更（重启）：旧在途请求按取消处理，不再写回结果
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token)
                    {
                        webData.CompletionSource.TrySetCanceled();
                        return;
                    }

                    m_SendingProtoBufList.Remove(webData);
                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsUnityWebRequestTimeout(unityWebRequest))
                            webData.CompletionSource.TrySetException(new TimeoutException(unityWebRequest.error));
                        else
                            webData.CompletionSource.TrySetException(new Exception(unityWebRequest.error));
                        return;
                    }

                    webData.CompletionSource.TrySetResult(new WebBufferResult(webData.UserData, unityWebRequest.downloadHandler.data));
                }
                finally
                {
                    unityWebRequest.Dispose(); // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                }
            };
        }

        /// <summary>
        /// 发送Post请求。
        /// </summary>
        /// <param name="url">目标服务器的URL地址。</param>
        /// <param name="message">要发送的消息对象，必须继承自MessageObject。</param>
        /// <typeparam name="T">返回的数据类型，必须继承自MessageObject并且实现IResponseMessage接口。</typeparam>
        /// <returns>返回一个任务对象，该任务完成时将包含从服务器接收到的响应数据，数据类型为T。</returns>
        /// <remarks>
        /// 此方法用于向指定的URL发送POST请求，并接收响应。请求的消息体由参数message提供，而响应则会被解析为指定的泛型类型T。
        /// </remarks>
        public async UniTask<T> Post<T>(string url, MessageObject message) where T : MessageObject, IResponseMessage
        {
            var webBufferResult = await PostInner(url, message);
            if (!webBufferResult.IsNotNull()) return default;
            var messageObjectHttp = SerializerHelper.Deserialize<MessageHttpObject>(webBufferResult.Result);
            if (!messageObjectHttp.IsNotNull() || messageObjectHttp.Id == default) return default;

            var messageType = ProtoMessageIdHandler.GetRespTypeById(messageObjectHttp.Id);
            if (messageType != typeof(T))
            {
                FuLogger.LogError($"Response message type is invalid. Expected '{typeof(T).FullName}', actual '{messageType.FullName}'.");
                return default;
            }

            return SerializerHelper.Deserialize<T>(messageObjectHttp.Body);
        }

        /// <summary>
        /// 内部Post请求处理方法
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="message">消息对象</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        private UniTask<WebBufferResult> PostInner(string url, MessageObject message, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, null);
            var id = ProtoMessageIdHandler.GetReqMessageIdByType(message.GetType());
            var messageHttpObject = new MessageHttpObject
            {
                Id       = id,
                UniqueId = message.UniqueId,
                Body     = SerializerHelper.Serialize(message),
            };
            var sendData = SerializerHelper.Serialize(messageHttpObject);
            var webData  = new WebProtoBufData(url, sendData, uniTaskCompletionSource, userData);
            m_WaitingProtoBufQueue.Enqueue(webData);
            return uniTaskCompletionSource.Task;
        }
    }
}
