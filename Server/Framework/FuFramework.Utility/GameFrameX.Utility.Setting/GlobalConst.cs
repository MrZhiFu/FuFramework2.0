namespace FuFramework.Utility.Setting;

/// <summary>
/// 全局常量类
/// </summary>
public static class GlobalConst
{
	/// <summary>
	/// SessionId Key
	/// </summary>
	public const string SessionIdKey = "SESSION_ID";

	/// <summary>
	/// ActorId Key
	/// </summary>
	public const string ActorIdKey = "ACTOR_ID";

	/// <summary>
	/// 唯一ID
	/// </summary>
	public const string UniqueIdIdKey = "UNIQUEID_ID";

	/// <summary>
	/// 组件代理名称后缀
	/// </summary>
	public const string ComponentAgentNameSuffix = "ComponentAgent";

	/// <summary>
	/// 组件处理器名称后缀
	/// </summary>
	public const string ComponentHandlerNameSuffix = "Handler";

	/// <summary>
	/// 事件处理器名称后缀
	/// </summary>
	public const string EventListenerNameSuffix = "EventListener";

	/// <summary>
	/// 组件代理包裹名称后缀
	/// </summary>
	public const string ComponentAgentWrapperNameSuffix = "ComponentAgentWrapper";

	/// <summary>
	/// 组件包裹名称后缀
	/// </summary>
	public const string WrapperNameSuffix = "Wrapper";

	/// <summary>
	/// 组件代理名称前缀
	/// </summary>
	public const string HotfixNameSpaceNamePrefix = "FuFramework.Hotfix.";

	/// <summary>
	/// 秒标记
	/// </summary>
	public const int SecondMask = 1073741823;

	/// <summary>
	/// 最大全局ID
	/// </summary>
	public const int MaxGlobalId = 9999000;

	/// <summary>
	/// 最小服务器ID
	/// </summary>
	public const int MinServerId = 1000;

	/// <summary>
	/// 最大服务器ID
	/// </summary>
	public const int MaxServerId = 9999;

	/// <summary>
	/// 最大Actor增量
	/// </summary>
	public const int MaxActorIncrease = 4095;

	/// <summary>
	/// 最大唯一增量
	/// </summary>
	public const int MaxUniqueIncrease = 524287;

	/// <summary>
	/// 服务器ID 长度标记位=&gt;49 = 63-14
	/// </summary>
	public const int ServerIdOrModuleIdMask = 49;

	/// <summary>
	/// Actor类型标记
	/// </summary>
	public const int ActorTypeMask = 42;

	/// <summary>
	/// 时间戳标记
	/// </summary>
	public const int TimestampMask = 12;

	/// <summary>
	/// 模块ID时间戳标记
	/// </summary>
	public const int ModuleIdTimestampMask = 19;

	/// <summary>
	/// 空将会被判断为无效值
	/// </summary>
	public const ushort ActorTypeNone = 0;

	/// <summary>
	/// 角色
	/// </summary>
	public const ushort ActorTypePlayer = 1;

	/// <summary>
	/// 分割线(勿调整,勿用于业务逻辑)
	/// </summary>
	public const int ActorTypeSeparator = 128;

	/// <summary>
	/// 服务类型
	/// </summary>
	public const int ActorTypeServer = 129;

	/// <summary>
	/// 最大值
	/// </summary>
	public const int ActorTypeMax = 999;

	/// <summary>
	/// HTTP 请求的签名字段名称
	/// </summary>
	public const string HttpSignKey = "sign";

	/// <summary>
	/// HTTP 请求的时间戳字段名称
	/// </summary>
	public const string HttpTimestampKey = "timestamp";

	/// <summary>
	/// 数据存储间隔 单位 毫秒
	/// </summary>
	internal const int SaveIntervalInMilliSeconds = 300000;

	/// <summary>
	/// </summary>
	public const int MAGIC = 60;
}
