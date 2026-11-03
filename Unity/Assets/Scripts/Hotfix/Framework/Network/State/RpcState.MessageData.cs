using System;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 单次 RPC 调用的等待载体：以 UniTaskCompletionSource 承载 await，由 Reply（回包配对）、Cancel（断线/销毁）、超时三条路径终结等待。
	/// </summary>
	internal sealed class RpcMessageData
	{
		/// <summary>
		/// 超时时间。单位毫秒
		/// </summary>
		public int TimeoutTime { get; }

		/// <summary>
		/// 消耗的时间
		/// </summary>
		public long ElapseTime { get; private set; }

		/// <summary>
		/// 请求消息
		/// </summary>
		public IRequestMessage RequestMessage { get; }

		/// <summary>
		/// 底层任务完成源
		/// </summary>
		private readonly UniTaskCompletionSource<IResponseMessage> m_tcs;

		/// <summary>
		/// 等待的返回结果。UniTaskCompletionSource.Task 支持被多个调用方重复 await。
		/// </summary>
		public UniTask<IResponseMessage> Task => m_tcs.Task;

		/// <summary>
		/// 创建 RPC 消息数据对象
		/// </summary>
		/// <param name="requestMessage">请求消息</param>
		private RpcMessageData(IRequestMessage requestMessage)
		{
			RequestMessage = requestMessage;
			TimeoutTime    = NetworkModule.RPC_TIMEOUT_DEFAULT;
			m_tcs          = new UniTaskCompletionSource<IResponseMessage>();
		}

		/// <summary>
		/// 创建RPC 消息数据对象
		/// </summary>
		/// <param name="actorRequestMessage">请求消息</param>
		internal static RpcMessageData Create(IRequestMessage actorRequestMessage)
		{
			return new RpcMessageData(actorRequestMessage);
		}

		/// <summary>
		/// 设置等待的返回结果
		/// </summary>
		/// <param name="responseMessage"></param>
		public void Reply(IResponseMessage responseMessage)
		{
			m_tcs.TrySetResult(responseMessage);
		}

		/// <summary>
		/// 以取消异常终结本次等待（断线/销毁时由 RpcState.Dispose 调用），
		/// 避免调用方 await 永久悬挂。
		/// </summary>
		public void Cancel()
		{
			m_tcs.TrySetException(new OperationCanceledException("RPC 调用已取消。消息为：" + RequestMessage));
		}

		/// <summary>
		/// 增加时间。如果超时返回true
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		internal bool IncrementalElapseTime(long time)
		{
			ElapseTime += time;
			if (ElapseTime < TimeoutTime) return false;
			m_tcs.TrySetException(new TimeoutException("RPC 调用超时。消息为：" + RequestMessage));
			return true;
		}
	}
}