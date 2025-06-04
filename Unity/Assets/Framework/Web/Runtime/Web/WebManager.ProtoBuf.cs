using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using ProtoBuf;
#if UNITY_WEBGL
using UnityEngine.Networking;
#endif

namespace GameFrameX.Web.Runtime
{
    /// <summary>
    /// Web请求管理器的ProtoBuf部分实现
    /// </summary>
    public partial class WebManager : GameFrameworkModule, IWebManager
    {
        /// <summary>
        /// 等待处理的ProtoBuf请求队列
        /// </summary>
        private readonly Queue<WebProtoBufData> m_WaitingProtoBufQueue = new Queue<WebProtoBufData>(256);

        /// <summary>
        /// 正在处理的ProtoBuf请求列表
        /// </summary>
        private readonly List<WebProtoBufData> m_SendingProtoBufList = new List<WebProtoBufData>(16);

        /// <summary>
        /// ProtoBuf内容类型常量
        /// </summary>
        private const string ProtoBufContentType = "application/x-protobuf";

        /// <summary>
        /// 更新处理ProtoBuf请求队列
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位</param>
        void UpdateProtoBuf(float elapseSeconds, float realElapseSeconds)
        {
            lock (m_StringBuilder)
            {
                if (m_SendingProtoBufList.Count < MaxConnectionPerServer)
                {
                    if (m_WaitingProtoBufQueue.Count > 0)
                    {
                        var webProtoBufData = m_WaitingProtoBufQueue.Dequeue();

                        MakeProtoBufBytesRequest(webProtoBufData);

                        m_SendingProtoBufList.Add(webProtoBufData);
                    }
                }
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

            m_MemoryStream.Dispose();
        }

        /// <summary>
        /// 执行ProtoBuf字节请求
        /// </summary>
        /// <param name="webData">ProtoBuf请求数据</param>
        private async void MakeProtoBufBytesRequest(WebProtoBufData webData)
        {
#if UNITY_WEBGL
            UnityWebRequest unityWebRequest;
            if (webData.IsGet)
            {
                unityWebRequest = UnityWebRequest.Get(webData.URL);
            }
            else
            {
                unityWebRequest = UnityWebRequest.Post(webData.URL, string.Empty);
            }

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            {
                unityWebRequest.SetRequestHeader("Content-Type", ProtoBufContentType);
                byte[] postData = webData.SendData;
                unityWebRequest.uploadHandler = new UploadHandlerRaw(postData);
            }

            var asyncOperation = unityWebRequest.SendWebRequest();
            asyncOperation.completed += (asyncOperation2) =>
            {
                m_SendingProtoBufList.Remove(webData);
                if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError || unityWebRequest.error != null)
                {
                    webData.Task.TrySetException(new Exception(unityWebRequest.error));
                    return;
                }

                webData.Task.SetResult(new WebBufferResult(webData.UserData, unityWebRequest.downloadHandler.data));
            };
#else
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(webData.URL);
                request.Method = webData.IsGet ? WebRequestMethods.Http.Get : WebRequestMethods.Http.Post;
                request.Timeout = (int)RequestTimeout.TotalMilliseconds; // 设置请求超时时间
                request.ContentType = ProtoBufContentType;
                byte[] postData = webData.SendData;
                request.ContentLength = postData.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    await requestStream.WriteAsync(postData, 0, postData.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        m_MemoryStream.SetLength(response.ContentLength);
                        m_MemoryStream.Position = 0;
                        await responseStream.CopyToAsync(m_MemoryStream);
                        webData.Task.SetResult(new WebBufferResult(webData.UserData, m_MemoryStream.ToArray())); // 将流的内容复制到内存流中并转换为byte数组 
                    }
                }
            }
            catch (WebException e)
            {
                // 捕获超时异常
                if (e.Status == WebExceptionStatus.Timeout)
                {
                    webData.Task.SetException(new TimeoutException(e.Message));
                    return;
                }

                webData.Task.SetException(e);
            }
            catch (IOException e)
            {
                webData.Task.SetException(e);
            }
            catch (Exception e)
            {
                webData.Task.SetException(e);
            }
            finally
            {
                m_SendingProtoBufList.Remove(webData);
            }
#endif
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
        public async Task<T> Post<T>(string url, MessageObject message) where T : MessageObject, IResponseMessage
        {
            var webBufferResult = await PostInner(url, message);
            if (webBufferResult.IsNotNull())
            {
                var messageObjectHttp = SerializerHelper.Deserialize<MessageHttpObject>(webBufferResult.Result);
                if (messageObjectHttp.IsNotNull() && messageObjectHttp.Id != default)
                {
                    var messageType = ProtoMessageIdHandler.GetRespTypeById(messageObjectHttp.Id);
                    if (messageType != typeof(T))
                    {
                        Log.Error($"Response message type is invalid. Expected '{typeof(T).FullName}', actual '{messageType.FullName}'.");
                        return default;
                    }

                    return SerializerHelper.Deserialize<T>(messageObjectHttp.Body);
                }
            }

            return default;
        }

        /// <summary>
        /// 内部Post请求处理方法
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="message">消息对象</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        private Task<WebBufferResult> PostInner(string url, MessageObject message, object userData = null)
        {
            var uniTaskCompletionSource = new TaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, null);
            var id = ProtoMessageIdHandler.GetReqMessageIdByType(message.GetType());
            MessageHttpObject messageHttpObject = new MessageHttpObject
            {
                Id = id,
                UniqueId = message.UniqueId,
                Body = SerializerHelper.Serialize(message),
            };
            var sendData = SerializerHelper.Serialize(messageHttpObject);
            var webData = new WebProtoBufData(url, sendData, uniTaskCompletionSource, userData);
            m_WaitingProtoBufQueue.Enqueue(webData);
            return uniTaskCompletionSource.Task;
        }
    }
}