// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 连接流程状态：随异步连接发起而创建，携带 Socket 与用户自定义数据，供连接回调取回上下文。
	/// </summary>
	internal sealed class ConnectState
	{
		/// <summary>
		/// 用户自定义数据
		/// </summary>
		public object UserData { get; }

		/// <summary>
		/// Socket
		/// </summary>
		public INetworkSocket Socket { get; }

		/// <summary>
		/// 创建连接状态
		/// </summary>
		/// <param name="socket">网络 Socket</param>
		/// <param name="userData">用户自定义数据</param>
		public ConnectState(INetworkSocket socket, object userData)
		{
			Socket   = socket;
			UserData = userData;
		}
	}
}