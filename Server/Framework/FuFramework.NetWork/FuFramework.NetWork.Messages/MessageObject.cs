using System;
using System.Text;
using System.Text.Json.Serialization;
using FuFramework.Foundation.Json;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.Utility;
using FuFramework.Utility.Extensions;
using ProtoBuf;

namespace FuFramework.NetWork.Messages;

/// <summary>
/// 消息对象
/// </summary>
[ProtoContract]
public abstract class MessageObject : INetworkMessage, IMessage
{
	/// <summary>
	/// 消息ID
	/// </summary>
	[JsonIgnore]
	public int MessageId { get; private set; }

	/// <summary>
	/// 消息业务类型
	/// </summary>
	[JsonIgnore]
	public MessageOperationType OperationType { get; private set; }

	/// <summary>
	/// 消息的唯一ID
	/// </summary>
	[JsonIgnore]
	public int UniqueId { get; set; }

	/// <summary>
	/// </summary>
	protected MessageObject()
	{
		UpdateUniqueId();
	}

	/// <summary>
	/// 设置消息ID
	/// </summary>
	/// <param name="messageId"></param>
	public void SetMessageId(int messageId)
	{
		MessageId = messageId;
	}

	/// <summary>
	/// 更新唯一消息ID
	/// </summary>
	public void UpdateUniqueId()
	{
		UniqueId = IdGenerator.GetNextUniqueIntId();
	}

	/// <summary>
	/// 设置唯一消息ID
	/// </summary>
	/// <param name="uniqueId"></param>
	public void SetUniqueId(int uniqueId)
	{
		UniqueId = uniqueId;
	}

	/// <summary>
	/// 获取格式化后的消息字符串
	/// </summary>
	/// <returns></returns>
	public string ToFormatMessageString()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Clear();
			stringBuilder.AppendLine();
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
			handler.AppendFormatted('↓'.RepeatChar(140));
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("---MessageType:[");
			handler.AppendFormatted(GetType().Name.CenterAlignedText(30));
			handler.AppendLiteral("]");
			stringBuilder4.Append(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder2);
			handler.AppendLiteral("--MsgId:[");
			handler.AppendFormatted(MessageId.ToString().CenterAlignedText(11));
			handler.AppendLiteral("](");
			handler.AppendFormatted(MessageIdUtility.GetMainId(MessageId).ToString().CenterAlignedText(6));
			handler.AppendLiteral(",");
			handler.AppendFormatted(MessageIdUtility.GetSubId(MessageId).ToString().CenterAlignedText(6));
			handler.AppendLiteral(")");
			stringBuilder5.Append(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("--OpType:[");
			handler.AppendFormatted(OperationType.ToString().CenterAlignedText(20));
			handler.AppendLiteral("]");
			stringBuilder6.Append(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("--UniqueId:[");
			handler.AppendFormatted(UniqueId.ToString().CenterAlignedText(13));
			handler.AppendLiteral("]---");
			stringBuilder7.Append(ref handler);
			stringBuilder.AppendLine();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
			handler.AppendFormatted(ToJsonString());
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
			handler.AppendFormatted('↑'.RepeatChar(140));
			stringBuilder9.AppendLine(ref handler);
			stringBuilder.AppendLine();
			return StringBuilderCache.GetStringAndRelease(stringBuilder);
		}
		catch (Exception exception)
		{
			LogHelper.Error(exception);
		}
		return string.Empty;
	}

	/// <summary>
	/// 获取JSON格式化后的消息字符串
	/// </summary>
	/// <returns></returns>
	public string ToJsonString()
	{
		return JsonHelper.SerializeFormat(this);
	}

	/// <summary>
	/// 设置消息业务类型
	/// </summary>
	/// <param name="messageOperationType">消息业务类型 </param>
	public void SetOperationType(MessageOperationType messageOperationType)
	{
		OperationType = messageOperationType;
	}

	/// <summary>
	/// 转换为字符串
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return JsonHelper.Serialize(this);
	}
}
