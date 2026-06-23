namespace FuFramework.DataBase.Abstractions;

/// <summary>
/// 软删除标记
/// </summary>
public interface ISafeDelete
{
	/// <summary>
	/// 是否删除
	/// </summary>
	bool IsDeleted { get; set; }

	/// <summary>
	/// 删除时间
	/// </summary>
	long DeleteTime { get; set; }
}
