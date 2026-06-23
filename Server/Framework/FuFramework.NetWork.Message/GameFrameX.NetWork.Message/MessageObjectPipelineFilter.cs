using System.Buffers;
using FuFramework.NetWork.Abstractions;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Message;

/// <summary>
/// 消息对象流水线过滤处理器
/// </summary>
public sealed class MessageObjectPipelineFilter : PipelineFilterBase<IMessage>
{
	/// <summary>
	/// 解析函数
	/// </summary>
	/// <param name="reader"></param>
	/// <returns></returns>
	public override IMessage Filter(ref SequenceReader<byte> reader)
	{
		ReadOnlySequence<byte> sequence = reader.Sequence;
		reader.TryPeekBigEndian(out uint value);
		if (value == 0)
		{
			reader.AdvanceToEnd();
			return null;
		}
		ReadOnlySequence<byte> sequence2 = sequence.Slice(sequence.Start, value);
		if (reader.Remaining < value)
		{
			reader.AdvanceToEnd();
		}
		else
		{
			reader.Advance(value);
		}
		return MessageHelper.DecoderHandler.Handler(ref sequence2);
	}
}
