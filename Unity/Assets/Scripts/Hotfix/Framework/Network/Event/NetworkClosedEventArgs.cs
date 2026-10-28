using Hotfix.Framework.Event;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络连接关闭事件。
	/// </summary>
	public sealed class NetworkClosedEventArgs : GameEventArgs
	{
		/// <summary>
		/// 获取网络连接关闭事件编号。
		/// </summary>
		public override string Id => EventId;

		/// <summary>
		/// 网络连接关闭事件编号。
		/// </summary>
		public static readonly string EventId = typeof(NetworkClosedEventArgs).FullName;

		/// <summary>
		/// 获取网络频道。
		/// </summary>
		public INetworkChannel NetworkChannel { get; private set; }

		/// <summary>
		/// 关闭原因（主动关闭 / 异常断线；业务据此决定是否自动重连）
		/// </summary>
		public ENetworkCloseReason CloseReason { get; private set; }

		/// <summary>
		/// 创建网络连接关闭事件。
		/// </summary>
		/// <param name="networkChannel">网络频道。</param>
		/// <param name="closeReason">关闭原因。</param>
		/// <returns>创建的网络连接关闭事件。</returns>
		public static NetworkClosedEventArgs Create(INetworkChannel networkChannel, ENetworkCloseReason closeReason)
		{
			var networkClosedEventArgs = ReferencePool.Acquire<NetworkClosedEventArgs>();
			networkClosedEventArgs.NetworkChannel = networkChannel;
			networkClosedEventArgs.CloseReason    = closeReason;
			return networkClosedEventArgs;
		}

		/// <summary>
		/// 清理网络连接关闭事件。
		/// </summary>
		public override void Clear()
		{
			NetworkChannel = null;
			CloseReason    = ENetworkCloseReason.Active;
		}
	}
}