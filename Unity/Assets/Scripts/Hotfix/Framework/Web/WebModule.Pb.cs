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
    ///     1. 处理 Pb 请求的出队、构建、发送与完成回调写回。
    ///     2. 负责清空 Pb 请求队列与列表。
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
                // 构造失败（非法 URL/Header 等）已回写异常并返回 false，不入发送列表
                if (SendPbReq(webPbData))
                    m_SendingPbList.Add(webPbData);
            }
        }

        /// <summary>
        /// 清空 Pb 请求队列与列表，取消未完成的任务。
        /// </summary>
        private void ClearPbReq()
        {
            while (m_WaitingPbQueue.Count > 0)
            {
                var webData = m_WaitingPbQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingPbQueue.Clear();

            while (m_SendingPbList.Count > 0)
            {
                var webData = m_SendingPbList[0];
                m_SendingPbList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingPbList.Clear();
        }

        /// <summary>
        /// 构建并发送 Pb 请求，完成回调写回字节结果。
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
                webData.CompletionSource.TrySetException(e);
                return false;
            }

            // 构建 + 发送成功后才登记在途：失败路径无在途登记，计数不泄漏
            var capturedToken = m_Scope.Token;   // 发起时捕获生命周期 Token：模块销毁/重启（OnDispose Cancel）后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var sendingList   = m_SendingPbList; // 捕获发送列表引用，完成回调不捕获 this（模块实例）
            var inFlight      = m_Scope.Begin(); // 登记在途：CancelAsync 等待本请求清理完毕

            // 任一 token 取消即 Abort 中断传输，避免在途请求等到超时才被回收；完成回调中注销。
            // 模块 scope token 恒可取消；仅在存在可取消 token 时创建链接源，减少无谓分配。
            CancellationTokenSource linkedCts         = null;
            var                     abortRegistration = default(CancellationTokenRegistration);
            if (capturedToken.CanBeCanceled || webData.Token.CanBeCanceled)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(capturedToken, webData.Token);
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
                    sendingList.Remove(webData);

                    // 模块销毁/重启（旧 scope 于 OnDispose Cancel，capturedToken 已触发）或调用方取消：按取消处理，不再写回结果。
                    // 注：重启时序 DisposeModules（OnDispose→Cancel）恒先于重新初始化，故无需再比对实时 m_Scope.Token。
                    if (capturedToken.IsCancellationRequested || webData.Token.IsCancellationRequested)
                    {
                        webData.CompletionSource.TrySetCanceled();
                        return;
                    }

                    if (requestRef.result != UnityWebRequest.Result.Success)
                    {
                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsTimeout(requestRef))
                            webData.CompletionSource.TrySetException(new TimeoutException(requestRef.error));
                        else
                            webData.CompletionSource.TrySetException(new Exception(requestRef.error));
                        return;
                    }

                    webData.CompletionSource.TrySetResult(new WebBufferResult(webData.UserData, requestRef.downloadHandler.data));
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
            url = UrlHandler(url, null);
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