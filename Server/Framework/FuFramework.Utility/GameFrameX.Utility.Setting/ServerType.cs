using System;

namespace FuFramework.Utility.Setting;

/// <summary>
/// 服务器类型
/// </summary>
[Flags]
public enum ServerType
{
	/// <summary>
	/// 空值
	/// </summary>
	None = -1,
	/// <summary>
	/// 客户端
	/// </summary>
	Client = 0,
	/// <summary>
	/// 日志服
	/// </summary>
	Log = 2,
	/// <summary>
	/// 数据库
	/// </summary>
	DataBase = 4,
	/// <summary>
	/// 缓存服
	/// </summary>
	Cache = 8,
	/// <summary>
	/// 网关服
	/// </summary>
	Gateway = 0x10,
	/// <summary>
	/// 账号服
	/// </summary>
	Account = 0x20,
	/// <summary>
	/// 路由
	/// </summary>
	Router = 0x40,
	/// <summary>
	/// 服务发现中心服，用于发现其他服务器。
	/// </summary>
	DiscoveryCenter = 0x80,
	/// <summary>
	/// 远程备份
	/// </summary>
	Backup = 0x100,
	/// <summary>
	/// 登录服务器
	/// </summary>
	Login = 0x200,
	/// <summary>
	/// 游戏服
	/// </summary>
	Game = 0x400,
	/// <summary>
	/// 匹配服
	/// </summary>
	Match = 0x800,
	/// <summary>
	/// 充值服
	/// </summary>
	Recharge = 0x1000,
	/// <summary>
	/// 逻辑服
	/// </summary>
	Logic = 0x2000,
	/// <summary>
	/// 聊天服
	/// </summary>
	Chat = 0x4000,
	/// <summary>
	/// 邮件服
	/// </summary>
	Mail = 0x8000,
	/// <summary>
	/// 公会服
	/// </summary>
	Guild = 0x10000,
	/// <summary>
	/// 房间服
	/// </summary>
	Room = 0x20000,
	/// <summary>
	/// 全部
	/// </summary>
	All = 0x3F5BE
}
