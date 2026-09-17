using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using FuFramework.Foundation.Logger;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Abstractions;

/// <summary>
/// 协议消息处理器
/// </summary>
public static class MessageProtoHelper
{
	private static readonly BidirectionalDictionary<int, Type> RequestDictionary = new BidirectionalDictionary<int, Type>();

	private static readonly BidirectionalDictionary<int, Type> AllMessageDictionary = new BidirectionalDictionary<int, Type>();

	private static readonly BidirectionalDictionary<int, Type> ResponseDictionary = new BidirectionalDictionary<int, Type>();

	private static readonly ConcurrentDictionary<Type, MessageOperationType> OperationType = new ConcurrentDictionary<Type, MessageOperationType>();

	private static readonly List<Type> HeartBeatList = new List<Type>();

	/// <summary>
	/// 获取消息ID,如果没有找到则返回 -1
	/// </summary>
	/// <param name="message">消息对象</param>
	/// <returns></returns>
	public static int GetMessageIdByType(INetworkMessage message)
	{
		message.CheckNotNull("message");
		return GetMessageIdByType(message.GetType());
	}

	/// <summary>
	/// 获取消息ID,如果没有找到则返回 -1
	/// </summary>
	/// <param name="type">消息类型</param>
	/// <returns></returns>
	public static int GetMessageIdByType(Type type)
	{
		if (AllMessageDictionary.TryGetKey(type, out var key))
		{
			return key;
		}
		return -1;
	}

	/// <summary>
	/// 获取消息类型，如果没有则返回null
	/// </summary>
	/// <param name="messageId">消息ID</param>
	/// <returns></returns>
	public static Type GetMessageTypeById(int messageId)
	{
		AllMessageDictionary.TryGetValue(messageId, out var value);
		return value;
	}

	/// <summary>
	/// 获取消息操作类型
	/// </summary>
	/// <param name="message">消息对象</param>
	/// <returns></returns>
	public static MessageOperationType GetMessageOperationType(INetworkMessage message)
	{
		message.CheckNotNull("message");
		return GetMessageOperationType(message.GetType());
	}

	/// <summary>
	/// 获取消息操作类型
	/// </summary>
	/// <param name="type">消息类型</param>
	/// <returns></returns>
	public static MessageOperationType GetMessageOperationType(Type type)
	{
		if (IsHeartbeat(type))
		{
			return MessageOperationType.HeartBeat;
		}
		if (OperationType.TryGetValue(type, out var value))
		{
			return value;
		}
		return MessageOperationType.None;
	}

	/// <summary>
	/// 设置消息ID和操作类型
	/// </summary>
	/// <param name="message">消息对象</param>
	public static void SetMessageId(INetworkMessage message)
	{
		message.CheckNotNull("message");
		Type type = message.GetType();
		message.SetMessageId(GetMessageIdByType(type));
	}

	/// <summary>
	/// 获取消息类型是否是心跳类型
	/// </summary>
	/// <param name="message">消息对象</param>
	/// <returns></returns>
	public static bool IsHeartbeat(INetworkMessage message)
	{
		message.CheckNotNull("message");
		return IsHeartbeat(message.GetType());
	}

	/// <summary>
	/// 获取消息类型是否是心跳类型
	/// </summary>
	/// <param name="type">消息类型</param>
	/// <returns></returns>
	public static bool IsHeartbeat(Type type)
	{
		return HeartBeatList.Contains(type);
	}

	/// <summary>
	/// 初始化所有协议对象
	/// </summary>
	/// <param name="assemblies">协议所在程序集集合.将在集合中查找所有的类型进行识别</param>
	/// <exception cref="T:System.Exception">如果ID重复将会触发异常</exception>
	public static void Init(params Assembly[] assemblies)
	{
		assemblies.CheckNotNull("assemblies");
		AllMessageDictionary.Clear();
		RequestDictionary.Clear();
		ResponseDictionary.Clear();
		HeartBeatList.Clear();
		OperationType.Clear();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type[] types = assemblies[i].GetTypes();
			foreach (Type type in types)
			{
				if (!(type.GetCustomAttribute(typeof(MessageTypeHandlerAttribute)) is MessageTypeHandlerAttribute messageTypeHandlerAttribute))
				{
					continue;
				}
				if (!AllMessageDictionary.TryAdd(messageTypeHandlerAttribute.MessageId, type))
				{
					RequestDictionary.TryGetValue(messageTypeHandlerAttribute.MessageId, out var value);
					throw new AlreadyArgumentException($"消息Id重复==>当前ID:{messageTypeHandlerAttribute.MessageId},已有ID类型:{value.FullName}");
				}
				OperationType.TryAdd(type, messageTypeHandlerAttribute.OperationType);
				if (type.IsImplWithInterface(typeof(IHeartBeatMessage)))
				{
					if (HeartBeatList.Contains(type))
					{
						LogHelper.Error("心跳消息重复==>类型:" + type.FullName);
					}
					else
					{
						HeartBeatList.Add(type);
					}
				}
				if (type.IsImplWithInterface(typeof(IRequestMessage)))
				{
					if (!RequestDictionary.TryAdd(messageTypeHandlerAttribute.MessageId, type))
					{
						RequestDictionary.TryGetValue(messageTypeHandlerAttribute.MessageId, out var value2);
						throw new AlreadyArgumentException($"请求Id重复==>当前ID:{messageTypeHandlerAttribute.MessageId},已有ID类型:{value2.FullName}");
					}
				}
				else if ((type.IsImplWithInterface(typeof(IResponseMessage)) || type.IsImplWithInterface(typeof(INotifyMessage))) && !ResponseDictionary.TryAdd(messageTypeHandlerAttribute.MessageId, type))
				{
					ResponseDictionary.TryGetValue(messageTypeHandlerAttribute.MessageId, out var value3);
					throw new AlreadyArgumentException($"返回Id重复==>当前ID:{messageTypeHandlerAttribute.MessageId},已有ID类型:{value3.FullName}");
				}
			}
		}
	}
}
