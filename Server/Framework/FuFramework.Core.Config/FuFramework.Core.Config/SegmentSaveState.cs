namespace FuFramework.Core.Config;

/// <summary>
/// 分段保存状态结构
/// </summary>
public readonly struct SegmentSaveState
{
	/// <summary>
	/// 读取位置
	/// </summary>
	public int ReaderIndex { get; }

	/// <summary>
	/// 写入位置
	/// </summary>
	public int WriterIndex { get; }

	/// <summary>
	/// 构造函数
	/// </summary>
	/// <param name="readerIndex">读取位置</param>
	/// <param name="writerIndex">写入位置</param>
	public SegmentSaveState(int readerIndex, int writerIndex)
	{
		ReaderIndex = readerIndex;
		WriterIndex = writerIndex;
	}
}
