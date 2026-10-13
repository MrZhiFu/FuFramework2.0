// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络连接状态
	/// </summary>
	internal sealed class ConnectState
	{
		/// <summary>
		/// Socket
		/// </summary>
		public INetworkSocket Socket { get; }

		/// <summary>
		/// 用户自定义数据
		/// </summary>
		public object UserData { get; }

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