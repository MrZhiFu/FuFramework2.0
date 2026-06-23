namespace FuFramework.DataBase.Abstractions;

/// <summary>
/// 创建标记
/// </summary>
public interface ISafeCreate
{
	/// <summary>
	/// 创建人
	/// </summary>
	long CreateId { get; set; }

	/// <summary>
	/// 创建时间
	/// </summary>
	long CreateTime { get; set; }
}
