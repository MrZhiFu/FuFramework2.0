using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道接口。
	/// </summary>
	public interface INetworkChannel
	{
		/// <summary>
		/// 获取网络频道所使用的 Socket。
		/// </summary>
		INetworkSocket Socket { get; }

		/// <summary>
		/// 获取是否已连接。
		/// </summary>
		bool Connected { get; }

		/// <summary>
		/// 心跳提供者（游戏侧派生实现，经生成物注册；提供心跳参数与心跳包构造）
		/// </summary>
		HeartBeatProviderBase HeartBeatProvider { get; }

		/// <summary>
		/// 注册心跳提供者（覆盖频道默认的心跳间隔与判死阈值）
		/// </summary>
		/// <param name="handler">心跳提供者对象</param>
		void RegisterHeartBeatProvider(HeartBeatProviderBase handler);

		/// <summary>
		/// 设置忽略的消息打印列表（过滤高频消息的调试日志，需开启 ENABLE_NETWORK_REQ_LOG / ENABLE_NETWORK_RSP_LOG 宏）。
		/// </summary>
		/// <param name="sendIds">发送消息ID忽略列表。传 null 清空。</param>
		/// <param name="receiveIds">接收消息ID忽略列表。传 null 清空。</param>
		void SetIgnoreLogNetworkIds(List<int> sendIds, List<int> receiveIds);

		/// <summary>
		/// 连接到远程主机。
		/// </summary>
		/// <param name="address">远程主机的地址。</param>
		/// <param name="userData">用户自定义数据。</param>
		void Connect(Uri address, object userData = null);

		/// <summary>
		/// 关闭连接并释放所有相关资源。
		/// </summary>
		void Close();

		/// <summary>
		/// 向远程主机单向发送消息包（发完即忘，不等待服务器回复）。
		/// 消息仅加入发送队列即返回，由后续帧的 ProcessSend 异步序列化发出；
		/// 服务器若回包，作为一条独立消息走消息分发（DispatchNotify），与本次发送无程序上的关联。
		/// 适用于无回复预期的通知类消息（聊天发言、移动同步等）；一问一答请用 <see cref="Call{TResponse}"/>。
		/// </summary>
		/// <typeparam name="T">消息包类型。</typeparam>
		/// <param name="msgObj">要发送的消息包。</param>
		/// <example>
		/// <code>
		/// // 单向发言：入队即返回，不关心服务器响应
		/// channel.Send(new ReqChatSay { Content = "hello" });
		/// </code>
		/// </example>
		void Send<T>(T msgObj) where T : MessageObject;

		/// <summary>
		/// 向远程主机发起 RPC 调用：发送请求并挂起等待服务器响应（内部复用 Send 完成物理发送）。
		/// 配对由编译期（IRequestMessage&lt;TResponse&gt; 推断响应类型，传错请求/配错响应均编译不过）与
		/// 运行期（包头 UniqueId 侧信道，服务器回包时原样带回）共同保证；
		/// 超时或断线时挂起的等待以异常结束，不会永久悬挂。
		/// 适用于一问一答的消息（登录、查询等）；无回复预期的通知类消息请用 <see cref="Send{T}"/>。
		/// </summary>
		/// <typeparam name="TResponse">响应消息类型，从请求实现的 IRequestMessage&lt;TResponse&gt; 自动推断，无需显式指定。</typeparam>
		/// <param name="msgObj">请求消息对象（须为生成器产出的、实现映射接口的 Req 消息）。</param>
		/// <returns>服务器响应消息；响应类型与 TResponse 不符时抛出异常（而非返回 null）。</returns>
		/// <example>
		/// <code>
		/// // 一问一答：响应类型自动推断（示例取自 WinPlayerList.PlayerLoginAsync）
		/// var reqPlayerLogin  = new ReqPlayerLogin { Id = 1 };
		/// var respPlayerLogin = await channel.Call(reqPlayerLogin);
		/// PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;
		/// </code>
		/// </example>
		UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> msgObj) where TResponse : MessageObject, IResponseMessage;
	}
}