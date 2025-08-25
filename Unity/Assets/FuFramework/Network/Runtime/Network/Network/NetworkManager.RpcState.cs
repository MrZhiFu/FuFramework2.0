using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    public sealed partial class NetworkManager
    {
        public partial class RpcState : IDisposable
        {
            /// <summary>
            /// 等待回复处理对象字典
            /// </summary>
            private readonly ConcurrentDictionary<long, RpcMessageData> m_waitingReplyHandlingObjects = new();

            /// <summary>
            /// 删除等待中的处理器ID列表,由于超时导致的删除
            /// </summary>
            private readonly HashSet<long> m_removeReplyHandlingObjectIds = new();

            private EventHandler<MessageObject> m_rpcStartHandler;
            private EventHandler<MessageObject> m_rpcEndHandler;
            private EventHandler<MessageObject> m_rpcErrorHandler;
            private EventHandler<MessageObject> m_rpcErrorCodeHandler;
            private readonly int m_rpcTimeout;
            private bool m_disposed;

            public RpcState(int timeout)
            {
                m_rpcTimeout = timeout;
                if (m_rpcTimeout < 3000)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout), "RPC超时时间不能小于3000毫秒");
                }
            }

            public void Dispose()
            {
                if (m_disposed) return;
                m_waitingReplyHandlingObjects.Clear();
                m_removeReplyHandlingObjectIds.Clear();
                m_disposed = true;
            }
            
            /// <summary>
            /// 处理RPC回复消息。
            /// 此方法用于处理接收到的RPC回复消息，并触发相应的结束处理程序。
            /// </summary>
            /// <param name="message">要处理的消息对象，必须实现IResponseMessage接口。</param>
            /// <returns>如果成功处理回复消息，则返回true；否则返回false。</returns>
            public bool TryReply(MessageObject message)
            {
                if (!message.GetType().IsImplWithInterface(typeof(IResponseMessage))) return false;
                if (!m_waitingReplyHandlingObjects.TryRemove(message.UniqueId, out var messageActorObject)) return false;
                
                try
                {
                    var responseMessage = message as IResponseMessage;
                    messageActorObject.Reply(responseMessage);
                    m_rpcEndHandler?.Invoke(this, message);
                    if (responseMessage?.ErrorCode != default)
                    {
                        m_rpcErrorCodeHandler?.Invoke(this, message);
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal(e);
                }

                return true;

            }

            /// <summary>
            /// 调用RPC并等待返回结果。
            /// 此方法会发送一个请求消息并返回一个任务，任务将在收到响应时完成。
            /// 可能会引发超时异常。
            /// </summary>
            /// <param name="messageObject">要发送的消息对象，必须实现IRequestMessage接口。</param>
            /// <returns>返回一个任务，该任务在收到响应时完成，并返回IResponseMessage。</returns>
            public Task<IResponseMessage> Call(MessageObject messageObject)
            {
                if (m_waitingReplyHandlingObjects.TryGetValue(messageObject.UniqueId, out var messageActorObject))
                {
                    return messageActorObject.Task;
                }

                var defaultMessageActorObject = RpcMessageData.Create(messageObject as IRequestMessage, m_rpcTimeout);
                m_waitingReplyHandlingObjects.TryAdd(messageObject.UniqueId, defaultMessageActorObject);
                try
                {
                    m_rpcStartHandler?.Invoke(this, messageObject);
                }
                catch (Exception e)
                {
                    Log.Fatal(e);
                }

                return defaultMessageActorObject.Task;
            }

            /// <summary>
            /// 逻辑更新，处理计时和超时移除任务
            /// </summary>
            /// <param name="elapseSeconds"></param>
            /// <param name="realElapseSeconds"></param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (m_waitingReplyHandlingObjects.Count > 0)
                {
                    var elapseSecondsTime = (long)(elapseSeconds * 1000);
                    m_removeReplyHandlingObjectIds.Clear();
                    foreach (var handlingObject in m_waitingReplyHandlingObjects)
                    {
                        var isTimeout = handlingObject.Value.IncrementalElapseTime(elapseSecondsTime);
                        if (!isTimeout) continue;
                        m_removeReplyHandlingObjectIds.Add(handlingObject.Key);
                        try
                        {
                            m_rpcErrorHandler?.Invoke(this, handlingObject.Value.RequestMessage as MessageObject);
                        }
                        catch (Exception e)
                        {
                            Log.Fatal(e);
                        }
                    }
                }

                if (m_removeReplyHandlingObjectIds.Count > 0)
                {
                    foreach (var objectId in m_removeReplyHandlingObjectIds)
                    {
                        m_waitingReplyHandlingObjects.TryRemove(objectId, out _);
                    }

                    m_removeReplyHandlingObjectIds.Clear();
                }
            }

            /// <summary>
            /// 设置RPC错误Code的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            
            public void SetRPCErrorCodeHandler(EventHandler<MessageObject> handler)
            {
                FuGuard.NotNull(handler, nameof(handler));
                m_rpcErrorCodeHandler = handler;
            }

            /// <summary>
            /// 设置RPC错误的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            
            public void SetRPCErrorHandler(EventHandler<MessageObject> handler)
            {
                FuGuard.NotNull(handler, nameof(handler));
                m_rpcErrorHandler = handler;
            }

            /// <summary>
            /// 设置RPC开始的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            
            public void SetRPCStartHandler(EventHandler<MessageObject> handler)
            {
                FuGuard.NotNull(handler, nameof(handler));
                m_rpcStartHandler = handler;
            }

            /// <summary>
            /// 设置RPC结束的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            
            public void SetRPCEndHandler(EventHandler<MessageObject> handler)
            {
                FuGuard.NotNull(handler, nameof(handler));
                m_rpcEndHandler = handler;
            }
        }
    }
}