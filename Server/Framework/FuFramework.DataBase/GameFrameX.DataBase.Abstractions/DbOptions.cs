namespace FuFramework.DataBase.Abstractions;

/// <summary>
/// 数据库选项
/// </summary>
public sealed record DbOptions
{
	/// <summary>
	/// 数据库类型
	/// </summary>
	public string Type { get; init; }

	/// <summary>
	/// 连接字符串
	/// </summary>
	public string ConnectionString { get; init; }

	/// <summary>
	/// 数据库名称
	/// </summary>
	public string Name { get; init; }
}
