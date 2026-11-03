#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络管理模块——调试分部（仅编辑器调试使用）：消息重发的发起入口。
	/// 由 NetworkDebug.Resend 编排调用；真正的响应/超时/取消终点复用既有 RpcState 链路，
	/// 结果自然落入监控记录列表（IsResend 标记）。
	/// </summary>
	public sealed partial class NetworkModule
	{
		/// <summary>
		/// 发起重发（调试专用）：经非泛型 CallForDebug 走完整 RPC 链路（Send + 挂表等待配对回包）。
		/// 频道不存在时记日志放弃；未连接由 CallForDebug 内的 IsOpen 守卫快速失败（protected 不可外部判）。
		/// </summary>
		/// <param name="msgObj">已由 JSON 反序列化出的请求消息对象（UniqueId 保持新分配值）</param>
		internal void ResendForDebug(MessageObject msgObj)
		{
			if (m_networkChannel == null)
			{
				FuLogger.LogWarning("NetworkDebug: 重发放弃，当前无网络频道。");
				NetworkDebug.ResetResendMark(); // 此路径不会产生发送埋点消费标记，防泄漏（审查 M-2）
				return;
			}

			m_networkChannel.CallForDebug(msgObj).Forget();
		}

		/// <summary>
		/// 触发断线（调试专用，供网络调试面板"触发断线"按钮）：以 Lost 语义关闭当前频道——
		/// 客户端视角等价于连接异常死亡，Closed(Lost) 事件驱动 ReconnectManager 进入自动重连编排。
		/// 用于测试断线重连而无需真实停服/断网。无频道时记日志放弃（未连接亦可触发：频道对象在即可）。
		/// </summary>
		internal void TriggerDisconnectForDebug()
		{
			if (m_networkChannel == null)
			{
				FuLogger.LogWarning("NetworkDebug: 触发断线放弃，当前无网络频道。");
				return;
			}

			m_networkChannel.Close(ENetworkCloseReason.Lost);
		}

		/// <summary>
		/// 获取当前连接状态文本（调试专用，供网络调试面板工具栏指示）：
		/// 未连接（无频道）/ 已连接 {远端地址} / 已断开（频道在但传输层已断）。
		/// 判活走传输层 Socket.IsConnected（IsOpen 为频道 protected 业务闸门，外部类不可访问；
		/// 两者差异仅在 Connected 仍 true 但 IsOpen 已 false 的拒绝发送窗口，调试指示可接受）。
		/// </summary>
		internal string GetConnectionStateForDebug()
		{
			if (m_networkChannel == null) return "未连接";

			var socket = m_networkChannel.Socket;
			if (socket == null || !socket.IsConnected) return "已断开";

			return $"已连接 {socket.RemoteEndPoint}";
		}
	}
}
#endif
