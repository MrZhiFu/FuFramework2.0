using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 协议消息种类。
	/// 说明：一个消息类型可能同时是「请求」与「心跳」（例如 ReqHeartBeat），故用标志位表达。
	///      为与原实现保持一致，「请求 / 响应 / 推送」三个互斥位在生成期已按
	///      Request &gt; Response &gt; Notify 的优先级归一，不会同时出现。
	///      注册期仅心跳位参与运行时判定；其余角色由消息类型实现的角色接口自描述。
	/// </summary>
	[Flags]
	public enum EMessageKind
	{
		/// <summary>
		/// 未实现任何消息接口（原实现同样不会注册）。
		/// </summary>
		None = 0,

		/// <summary>
		/// 请求消息（IRequestMessage）
		/// </summary>
		Request = 1 << 0,

		/// <summary>
		/// 响应消息（IResponseMessage）
		/// </summary>
		Response = 1 << 1,

		/// <summary>
		/// 推送消息（INotifyMessage）
		/// </summary>
		Notify = 1 << 2,

		/// <summary>
		/// 心跳消息（IHeartBeatMessage）
		/// </summary>
		HeartBeat = 1 << 3,
	}

	/// <summary>
	/// 协议消息注册表。
	/// 功能：
	///     1. 记录「消息ID ↔ 消息类型」的唯一双向映射（Req/Resp/Notify 全部角色共用一张表）。
	///     2. 记录心跳消息类型集合。
	///
	/// 说明（项目铁律 4）：注册数据不再通过运行时扫描程序集 + 读取特性获得，
	/// 而是由生成物 <c>Register/AutoGen/MessageRegistry.g.cs</c> 静态调用 <see cref="Register{T}"/> 写入。
	/// 生成脚本：<c>Protobuf/gen-proto-registry.py</c>。
	///
	/// 合表说明：Req/Resp/Notify 共用同一 ID 编号空间（module &lt;&lt; 16 | 序号），
	/// 一张表使 ID 唯一性在 Register 期**全局校验**（跨角色撞号直接抛异常，启动期暴露）；
	/// Resp 与 Notify 不在表内区分——消息角色由类型自身实现的消息接口在运行期自描述
	/// （如 TryReply 的 is IResponseMessage、NotifyMessageHandlerAttribute 的注册期校验）。
	/// </summary>
	public static class MessageIdRegistry
	{
		/// <summary>
		/// 消息ID ↔ 类型 的唯一双向映射（含全部消息角色）。
		/// 仅启动期 RegisterAll 写入一次、之后只读，查询在主线程——普通 Dictionary 语义即可。
		/// </summary>
		private static readonly FuBidirectionalDictionary<int, Type> sr_messageDict = new();

		/// <summary>
		/// 心跳消息类型集合。
		/// 说明：IsHeartbeat 在每收发一个包时都可能被调用（默认包头处理器 / 派发前截流），
		/// 使用 HashSet 避免线性查找开销。
		/// </summary>
		private static readonly HashSet<Type> sr_heartBeatTypes = new();

		/// <summary>
		/// 清空全部注册数据（供初始化重试使用）。
		/// </summary>
		public static void Reset()
		{
			sr_messageDict.Clear();
			sr_heartBeatTypes.Clear();
		}

		/// <summary>
		/// 注册一个协议消息类型。
		/// 错误语义：消息 ID **全局唯一**（含跨角色）——重复 ID 或重复心跳类型直接抛异常（启动期暴露）。
		/// </summary>
		/// <param name="messageId">消息ID（模块 &lt;&lt; 16 | 序号）</param>
		/// <param name="kind">消息种类（当前仅心跳位参与运行时判定；Request/Response/Notify 位由消息类型实现的角色接口自描述）</param>
		/// <typeparam name="T">协议消息类型</typeparam>
		public static void Register<T>(int messageId, EMessageKind kind) where T : MessageObject
		{
			var type = typeof(T);

			if ((kind & EMessageKind.HeartBeat) != 0)
			{
				if (!sr_heartBeatTypes.Add(type))
					throw new InvalidOperationException($"心跳消息重复==>类型:{type.FullName}");
			}

			if (!sr_messageDict.TryAdd(messageId, type))
			{
				sr_messageDict.TryGetValueByKey(messageId, out var exist);
				throw new InvalidOperationException($"消息Id重复==>当前ID:{messageId},已有ID类型:{exist?.FullName ?? type.FullName}");
			}
		}

		/// <summary>
		/// 根据消息ID获取消息类型（收侧正查；未注册时返回 null）。
		/// </summary>
		public static Type GetTypeById(int messageId)
		{
			sr_messageDict.TryGetValueByKey(messageId, out var value);
			return value;
		}

		/// <summary>
		/// 根据消息类型获取消息ID（发侧反查；未注册时返回 0）。
		/// </summary>
		public static int GetIdByType(Type type)
		{
			sr_messageDict.TryGetKeyByValue(type, out var value);
			return value;
		}

		/// <summary>
		/// 获取消息类型是否是心跳类型。
		/// </summary>
		public static bool IsHeartbeat(Type type) => type != null && sr_heartBeatTypes.Contains(type);
	}
}
