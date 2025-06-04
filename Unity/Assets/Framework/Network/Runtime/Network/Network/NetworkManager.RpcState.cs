﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFrameX.Runtime;

namespace GameFrameX.Network.Runtime
{
    public sealed partial class NetworkManager
    {
        public partial class RpcState : IDisposable
        {
            /// <summary>
            /// 等待回复处理对象字典
            /// </summary>
            private readonly ConcurrentDictionary<long, RpcMessageData> _waitingReplyHandlingObjects = new ConcurrentDictionary<long, RpcMessageData>();

            /// <summary>
            /// 删除等待中的处理器ID列表,由于超时导致的删除
            /// </summary>
            private readonly HashSet<long> _removeReplyHandlingObjectIds = new HashSet<long>();

            private EventHandler<MessageObject> _rpcStartHandler;
            private EventHandler<MessageObject> _rpcEndHandler;
            private EventHandler<MessageObject> _rpcErrorHandler;
            private EventHandler<MessageObject> _rpcErrorCodeHandler;
            private readonly int _rpcTimeout;
            private bool _disposed;

            public RpcState(int timeout)
            {
                _rpcTimeout = timeout;
                if (_rpcTimeout < 3000)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout), "RPC超时时间不能小于3000毫秒");
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _waitingReplyHandlingObjects.Clear();
                _removeReplyHandlingObjectIds.Clear();
                _disposed = true;
            }


            /// <summary>
            /// 处理RPC回复消息。
            /// 此方法用于处理接收到的RPC回复消息，并触发相应的结束处理程序。
            /// </summary>
            /// <param name="message">要处理的消息对象，必须实现IResponseMessage接口。</param>
            /// <returns>如果成功处理回复消息，则返回true；否则返回false。</returns>
            public bool TryReply(MessageObject message)
            {
                if (message.GetType().IsImplWithInterface(typeof(IResponseMessage)))
                {
                    if (_waitingReplyHandlingObjects.TryRemove(message.UniqueId, out var messageActorObject))
                    {
                        try
                        {
                            var responseMessage = message as IResponseMessage;
                            messageActorObject.Reply(responseMessage);
                            _rpcEndHandler?.Invoke(this, message);
                            if (responseMessage?.ErrorCode != default)
                            {
                                _rpcErrorCodeHandler?.Invoke(this, message);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Fatal(e);
                        }

                        return true;
                    }
                }

                return false;
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
                if (_waitingReplyHandlingObjects.TryGetValue(messageObject.UniqueId, out var messageActorObject))
                {
                    return messageActorObject.Task;
                }

                var defaultMessageActorObject = RpcMessageData.Create(messageObject as IRequestMessage, _rpcTimeout);
                _waitingReplyHandlingObjects.TryAdd(messageObject.UniqueId, defaultMessageActorObject);
                try
                {
                    _rpcStartHandler?.Invoke(this, messageObject);
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
                if (_waitingReplyHandlingObjects.Count > 0)
                {
                    var elapseSecondsTime = (long)(elapseSeconds * 1000);
                    _removeReplyHandlingObjectIds.Clear();
                    foreach (var handlingObject in _waitingReplyHandlingObjects)
                    {
                        bool isTimeout = handlingObject.Value.IncrementalElapseTime(elapseSecondsTime);
                        if (isTimeout)
                        {
                            _removeReplyHandlingObjectIds.Add(handlingObject.Key);
                            try
                            {
                                _rpcErrorHandler?.Invoke(this, handlingObject.Value.RequestMessage as MessageObject);
                            }
                            catch (Exception e)
                            {
                                Log.Fatal(e);
                            }
                        }
                    }
                }

                if (_removeReplyHandlingObjectIds.Count > 0)
                {
                    foreach (var objectId in _removeReplyHandlingObjectIds)
                    {
                        _waitingReplyHandlingObjects.TryRemove(objectId, out _);
                    }

                    _removeReplyHandlingObjectIds.Clear();
                }
            }

            /// <summary>
            /// 设置RPC错误Code的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            [UnityEngine.Scripting.Preserve]
            public void SetRPCErrorCodeHandler(EventHandler<MessageObject> handler)
            {
                GameFrameworkGuard.NotNull(handler, nameof(handler));
                _rpcErrorCodeHandler = handler;
            }

            /// <summary>
            /// 设置RPC错误的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            [UnityEngine.Scripting.Preserve]
            public void SetRPCErrorHandler(EventHandler<MessageObject> handler)
            {
                GameFrameworkGuard.NotNull(handler, nameof(handler));
                _rpcErrorHandler = handler;
            }

            /// <summary>
            /// 设置RPC开始的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            [UnityEngine.Scripting.Preserve]
            public void SetRPCStartHandler(EventHandler<MessageObject> handler)
            {
                GameFrameworkGuard.NotNull(handler, nameof(handler));
                _rpcStartHandler = handler;
            }

            /// <summary>
            /// 设置RPC结束的处理函数
            /// </summary>
            /// <param name="handler">处理函数</param>
            [UnityEngine.Scripting.Preserve]
            public void SetRPCEndHandler(EventHandler<MessageObject> handler)
            {
                GameFrameworkGuard.NotNull(handler, nameof(handler));
                _rpcEndHandler = handler;
            }
        }
    }
}