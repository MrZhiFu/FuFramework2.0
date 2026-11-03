using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——对外 API 分部：心跳提供者注册、RPC 调用。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// RPC 状态
		/// </summary>
		private readonly RpcState m_rpcState;

		/// <summary>
		/// 心跳提供者（游戏侧派生实现，经生成物注册）
		/// </summary>
		public HeartBeatProviderBase HeartBeatProvider { get; private set; }

		/// <summary>
		/// 注册心跳提供者（覆盖频道默认的心跳间隔与判死阈值）
		/// </summary>
		/// <param name="handler">心跳提供者对象</param>
		public void RegisterHeartBeatProvider(HeartBeatProviderBase handler)
		{
			handler.NotNull(nameof(handler));
			HeartBeatProvider = handler; // 心跳参数由 ProcessHeartBeat 直读 Provider，无注册时快照
		}

		/// <summary>
		/// 向远程主机发起 RPC 调用：发送请求并挂起等待服务器响应。
		/// 说明：
		///     1. 第一步即调用 Send 完成物理发送（复用同一条发送管线）；
		///     2. 随后按 UniqueId 向 RpcState 登记等待，收到配对回包时唤醒并返回结果；
		///        超时或断线时以错误/取消结束。
		/// 详细说明与示例见 <see cref="INetworkChannel.Call{TResponse}"/>。
		/// </summary>
		/// <typeparam name="TResponse">响应消息类型，从请求实现的 IRequestMessage&lt;TResponse&gt; 自动推断。</typeparam>
		/// <param name="msgObj">请求消息对象（须为生成器产出的、实现映射接口的 Req 消息）。</param>
		/// <returns>服务器响应消息；响应类型与 TResponse 不符时抛出异常（而非返回 null）。</returns>
		public async UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> msgObj)
			where TResponse : MessageObject, IResponseMessage
		{
			msgObj.NotNull(nameof(msgObj));
			// 生成器产出的 Req 消息均为 MessageObject 子类；显式转换以便复用发送管线与 RPC 登记。
			// 顺序说明：此处先 Send 后登记不会丢配对——
			// Send 仅入队，序列化/网络发出/响应派发都在后续主线程阶段，登记在同一调用内先于任何响应消费完成。
			// 该安全性依赖「Call 必须在主线程调用」这一前提；若未来允许后台线程 Call，必须改为先登记再发包。
			Send((MessageObject)msgObj);
			var result = await m_rpcState.Call((MessageObject)msgObj);
			if (result is TResponse response) return response;

			// 编译期已锁定 Req→Resp 配对，此处只可能因服务器回包类型异常触发——显式失败优于原 as 的静默 null。
			throw new InvalidOperationException($"RPC 响应类型不匹配。请求:{msgObj.GetType().Name}, 期望响应:{typeof(TResponse).Name}, 实际:{result?.GetType().Name ?? "null"}");
		}

		/// <summary>
		/// 设置忽略的消息打印列表（用于过滤高频消息——如心跳、移动同步——的调试日志，
		/// 需开启 ENABLE_NETWORK_REQ_LOG / ENABLE_NETWORK_RSP_LOG 宏后生效）。
		/// </summary>
		/// <param name="sendIds">发送消息ID忽略列表。传 null 清空。</param>
		/// <param name="receiveIds">接收消息ID忽略列表。传 null 清空。</param>
		public void SetIgnoreLogNetworkIds(List<int> sendIds, List<int> receiveIds)
		{
			m_IgnoreSendIds    = sendIds    ?? new List<int>();
			m_IgnoreReceiveIds = receiveIds ?? new List<int>();
		}

#if UNITY_EDITOR
		/// <summary>
		/// 网络调试面板重发专用的非泛型 RPC 调用（仅供 NetworkDebug.Resend 编排使用）。
		/// 说明：运行时 Type 无法调泛型 <see cref="Call{TResponse}"/>（TResponse 编译期推断），
		/// 此为等价的非泛型路径——第一步 Send 复用发送管线，随后按 UniqueId 挂表等待配对回包。
		/// UniqueId 沿用消息对象新分配值（重发编排不重置），响应/超时/取消经既有 RpcState 终点自然入监控记录。
		/// </summary>
		/// <param name="msgObj">请求消息对象</param>
		/// <returns>服务器响应消息</returns>
		internal async UniTask<IResponseMessage> CallForDebug(MessageObject msgObj)
		{
			msgObj.NotNull(nameof(msgObj));
			try
			{
				if (!IsOpen) throw new InvalidOperationException("[NetworkDebug] 重发放弃：网络频道未连接。");
				Send(msgObj);
				return await m_rpcState.Call(msgObj);
			}
			catch (Exception e)
			{
				// 重发异步失败（未连接/发送异常）：把本次重发请求记录回写为错误态——列表可见痕迹；
				// Send 前失败则无记录产生，OnRpcSettled 按 UniqueId 查不到即静默忽略。
				// 同时复位一次性重发标记（失败路径没有发送埋点去消费它，会泄漏到下一条真实请求）（审查 M-2）。
				// 异常原样上抛，交 Forget 的未观察异常日志（保持既有行为）
				NetworkDebug.ResetResendMark();
				NetworkDebug.OnRpcSettled(msgObj.UniqueId, ENetworkMessageStatus.Error, 0);
				throw;
			}
		}
#endif
	}
}