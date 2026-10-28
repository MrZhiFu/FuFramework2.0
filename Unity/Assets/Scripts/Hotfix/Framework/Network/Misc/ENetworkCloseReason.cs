// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道关闭原因（区分主动关闭与异常断线，供业务层决定是否自动重连）。
	/// </summary>
	public enum ENetworkCloseReason
	{
		/// <summary>
		/// 主动关闭：登出、销毁、手动放弃、重建前清理
		/// </summary>
		Active = 0,

		/// <summary>
		/// 异常断线：心跳判死、收发异常、Socket 断开、服务器踢人/顶号。
		/// 注意：被顶号在此表现为 Lost（客户端只能被动感知断线）——与普通断线的区分
		/// 靠 <c>NotifyPrompt(Type=5)</c> 消息标记（见 ReconnectManager），不靠本枚举。
		/// </summary>
		Lost,
	}
}