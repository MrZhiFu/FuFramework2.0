﻿using System;
using System.Threading.Tasks;
using GameFrameX.Runtime;

namespace GameFrameX.Network.Runtime
{
    public partial class NetworkManager
    {
        public partial class RpcState : IDisposable
        {
            internal sealed class RpcMessageData : IDisposable
            {
                /// <summary>
                /// 消息的唯一ID
                /// </summary>
                
                public long UniqueId { get; }

                /// <summary>
                /// 创建时间
                /// </summary>
                public long CreatedTime { get; }

                /// <summary>
                /// 消耗的时间
                /// </summary>
                public long ElapseTime { get; private set; }

                /// <summary>
                /// 请求消息
                /// </summary>
                public IRequestMessage RequestMessage { get; private set; }

                /// <summary>
                /// 超时时间。单位毫秒
                /// </summary>
                public int Timeout { get; }

                /// <summary>
                /// 响应消息
                /// </summary>
                public IResponseMessage ResponseMessage { get; private set; }

                /// <summary>
                /// 设置等待的返回结果
                /// </summary>
                /// <param name="responseMessage"></param>
                public void Reply(IResponseMessage responseMessage)
                {
                    ResponseMessage = responseMessage;
                    m_Tcs.SetResult(responseMessage);
                }

                /// <summary>
                /// 增加时间。如果超时返回true
                /// </summary>
                /// <param name="time"></param>
                /// <returns></returns>
                internal bool IncrementalElapseTime(long time)
                {
                    ElapseTime += time;
                    if (ElapseTime >= Timeout)
                    {
                        m_Tcs.TrySetException(new TimeoutException("Rpc call timeout! Message is :" + RequestMessage));
                        return true;
                    }

                    return false;
                }

                /// <summary>
                /// 创建RPC 消息数据对象
                /// </summary>
                /// <param name="actorRequestMessage"></param>
                /// <param name="timeout"></param>
                /// <returns></returns>
                
                internal static RpcMessageData Create(IRequestMessage actorRequestMessage, int timeout = 5000)
                {
                    var defaultMessageActorObject = new RpcMessageData(actorRequestMessage, timeout);
                    return defaultMessageActorObject;
                }

                private RpcMessageData(IRequestMessage requestMessage, int timeout)
                {
                    CreatedTime = TimerHelper.UnixTimeMilliseconds();
                    RequestMessage = requestMessage;
                    Timeout = timeout;
                    UniqueId = ((MessageObject)requestMessage).UniqueId;
                    m_Tcs = new TaskCompletionSource<IResponseMessage>();
                }

                private readonly TaskCompletionSource<IResponseMessage> m_Tcs;

                public Task<IResponseMessage> Task
                {
                    get { return m_Tcs.Task; }
                }

                public void Dispose()
                {
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}