namespace FuFramework.Core.Config;

/// <summary>
/// 反序列化错误枚举
/// </summary>
public enum EDeserializeError
{
	/// <summary>
	/// 成功
	/// </summary>
	OK,
	/// <summary>
	/// 数据不足
	/// </summary>
	NOT_ENOUGH,
	/// <summary>
	/// 超出大小限制
	/// </summary>
	EXCEED_SIZE
}
