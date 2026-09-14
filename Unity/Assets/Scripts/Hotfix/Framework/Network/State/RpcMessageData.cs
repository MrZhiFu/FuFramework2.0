using System;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// RPC 消息数据对象
    /// </summary>
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
            m_Tcs.TrySetResult(responseMessage);
        }

        /// <summary>
        /// 以取消异常终结本次等待（断线/销毁时由 RpcState.Dispose 调用），
        /// 避免调用方 await 永久悬挂。
        /// </summary>
        public void Cancel()
        {
            m_Tcs.TrySetException(new OperationCanceledException("Rpc call canceled! Message is :" + RequestMessage));
        }

        /// <summary>
        /// 增加时间。如果超时返回true
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        internal bool IncrementalElapseTime(long time)
        {
            ElapseTime += time;
            if (ElapseTime < Timeout) return false;
            m_Tcs.TrySetException(new TimeoutException("Rpc call timeout! Message is :" + RequestMessage));
            return true;
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
            CreatedTime    = Utility.Time.ClientNow();
            RequestMessage = requestMessage;
            Timeout        = timeout;
            UniqueId       = ((MessageObject)requestMessage).UniqueId;
            m_Tcs          = new UniTaskCompletionSource<IResponseMessage>();
        }

        private readonly UniTaskCompletionSource<IResponseMessage> m_Tcs;

        /// <summary>
        /// 等待的返回结果。UniTaskCompletionSource.Task 支持被多个调用方重复 await。
        /// </summary>
        public UniTask<IResponseMessage> Task => m_Tcs.Task;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
