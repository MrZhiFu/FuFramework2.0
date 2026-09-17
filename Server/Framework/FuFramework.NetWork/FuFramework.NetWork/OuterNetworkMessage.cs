using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.ProtoBuf.Net;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork;

/// <summary>
/// 外部消息
/// </summary>
public sealed class OuterNetworkMessage : IOuterNetworkMessage, IMessage
{
	private readonly ConcurrentDictionary<string, object> _data = new ConcurrentDictionary<string, object>();

	/// <summary>
	/// 消息类型
	/// </summary>
	[JsonIgnore]
	public Type MessageType { get; private set; }

	/// <summary>
	/// 消息数据
	/// </summary>
	public byte[] MessageData { get; private set; }

	/// <summary>
	/// 消息头对象
	/// </summary>
	public INetworkMessageHeader Header { get; private set; }

	/// <summary>
	/// 消息唯一ID
	/// </summary>
	public string UniqueId { get; private set; }

	/// <summary>
	/// 转换消息数据为消息对象
	/// </summary>
	/// <returns></returns>
	public INetworkMessage DeserializeMessageObject()
	{
		INetworkMessage obj = (INetworkMessage)ProtoBufSerializerHelper.Deserialize(MessageData, MessageType);
		obj.SetUniqueId(Header.UniqueId);
		return obj;
	}

	/// <summary>
	/// 设置消息数据
	/// </summary>
	/// <param name="messageData"></param>
	public void SetMessageData(byte[] messageData)
	{
		MessageData = messageData;
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
			INetworkMessage networkMessage = DeserializeMessageObject();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("---MessageType:[");
			handler.AppendFormatted(networkMessage.GetType().Name.CenterAlignedText(30));
			handler.AppendLiteral("]");
			stringBuilder4.Append(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder2);
			handler.AppendLiteral("--MsgId:[");
			handler.AppendFormatted(Header.MessageId.ToString().CenterAlignedText(11));
			handler.AppendLiteral("](");
			handler.AppendFormatted(MessageIdUtility.GetMainId(Header.MessageId).ToString().CenterAlignedText(6));
			handler.AppendLiteral(",");
			handler.AppendFormatted(MessageIdUtility.GetSubId(Header.MessageId).ToString().CenterAlignedText(6));
			handler.AppendLiteral(")");
			stringBuilder5.Append(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("--OpType:[");
			handler.AppendFormatted(Header.OperationType.ToString().CenterAlignedText(20));
			handler.AppendLiteral("]");
			stringBuilder6.Append(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("--UniqueId:[");
			handler.AppendFormatted(Header.UniqueId.ToString().CenterAlignedText(13));
			handler.AppendLiteral("]---");
			stringBuilder7.Append(ref handler);
			stringBuilder.AppendLine();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
			handler.AppendFormatted(networkMessage.ToJsonString());
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
	/// 设置自定义数据
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public void SetData(string key, object value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// 获取自定义数据
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public object GetData(string key)
	{
		_data.TryGetValue(key, out var value);
		return value;
	}

	/// <summary>
	/// 清除自定义数据
	/// </summary>
	public void ClearData()
	{
		_data.Clear();
	}

	/// <summary>
	/// 设置消息头
	/// </summary>
	/// <param name="header"></param>
	public void SetMessageHeader(INetworkMessageHeader header)
	{
		Header = header;
	}

	/// <summary>
	/// 设置唯一消息ID
	/// </summary>
	/// <param name="uniqueId"></param>
	public void SetUniqueId(string uniqueId)
	{
		UniqueId = uniqueId;
	}

	/// <summary>
	/// 设置消息类型
	/// </summary>
	/// <param name="messageType"></param>
	public void SetMessageType(Type messageType)
	{
		MessageType = messageType;
	}

	/// <summary>
	/// 创建内部消息
	/// </summary>
	/// <param name="message"></param>
	/// <param name="messageObjectHeader"></param>
	/// <returns></returns>
	public static IOuterNetworkMessage Create(INetworkMessage message, INetworkMessageHeader messageObjectHeader)
	{
		OuterNetworkMessage outerNetworkMessage = new OuterNetworkMessage();
		outerNetworkMessage.SetMessageType(message.GetType());
		outerNetworkMessage.SetUniqueId(message.UniqueId.ToString());
		byte[] messageData = ProtoBufSerializerHelper.Serialize(message);
		outerNetworkMessage.SetMessageData(messageData);
		messageObjectHeader.OperationType = MessageProtoHelper.GetMessageOperationType(message.GetType());
		messageObjectHeader.MessageId = MessageProtoHelper.GetMessageIdByType(message.GetType());
		messageObjectHeader.UniqueId = message.UniqueId;
		outerNetworkMessage.SetMessageHeader(messageObjectHeader);
		return outerNetworkMessage;
	}

	/// <summary>
	/// 获取自定义数据
	/// </summary>
	/// <returns></returns>
	public Dictionary<string, object> GetData()
	{
		return _data.ToDictionary();
	}

	/// <summary>
	/// 删除自定义数据
	/// </summary>
	/// <param name="key"></param>
	public bool RemoveData(string key)
	{
		object value;
		return _data.Remove(key, out value);
	}

	/// <summary>
	/// 创建内部消息
	/// </summary>
	/// <param name="messageObjectHeader">消息头</param>
	/// <param name="messageData">消息体</param>
	/// <param name="messageType">消息体的类型</param>
	/// <returns></returns>
	public static OuterNetworkMessage Create(INetworkMessageHeader messageObjectHeader, byte[] messageData, Type messageType)
	{
		OuterNetworkMessage outerNetworkMessage = new OuterNetworkMessage();
		outerNetworkMessage.SetMessageHeader(messageObjectHeader);
		outerNetworkMessage.SetMessageData(messageData);
		outerNetworkMessage.SetMessageType(messageType);
		return outerNetworkMessage;
	}
}
