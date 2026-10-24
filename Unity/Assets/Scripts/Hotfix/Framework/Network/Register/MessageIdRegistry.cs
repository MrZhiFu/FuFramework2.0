using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 协议消息注册表。
	/// 功能：记录「消息ID ↔ 消息类型」的唯一双向映射（Req/Resp/Notify 全部角色共用一张表）。
	///
	/// 说明（项目铁律 4）：注册数据不再通过运行时扫描程序集 + 读取特性获得，
	/// 而是由生成物 <c>Register/AutoGen/MessageRegistry.g.cs</c> 静态调用 <see cref="Register{T}"/> 写入。
	/// 生成脚本：<c>Protobuf/gen-proto-registry.py</c>。
	///
	/// 合表说明：Req/Resp/Notify 共用同一 ID 编号空间（module &lt;&lt; 16 | 序号），
	/// 一张表使 ID 唯一性在 Register 期**全局校验**（跨角色撞号直接抛异常，启动期暴露）。
	/// 消息角色（含心跳）不在表内区分——由类型自身实现的消息接口在运行期自描述
	/// （is IResponseMessage / is IHeartBeatMessage 等，服务器侧同构）。
	/// </summary>
	public static class MessageIdRegistry
	{
		/// <summary>
		/// 消息ID ↔ 类型 的唯一双向映射（含全部消息角色）。
		/// 仅启动期 RegisterAll 写入一次、之后只读，查询在主线程——普通 Dictionary 语义即可。
		/// </summary>
		private static readonly FuBidirectionalDictionary<int, Type> sr_messageDict = new();

		/// <summary>
		/// 清空全部注册数据（供初始化重试使用）。
		/// </summary>
		public static void Reset()
		{
			sr_messageDict.Clear();
		}

		/// <summary>
		/// 注册一个协议消息类型。
		/// 错误语义：消息 ID **全局唯一**（含跨角色）——重复 ID 直接抛异常（启动期暴露）。
		/// </summary>
		/// <param name="messageId">消息ID（模块 &lt;&lt; 16 | 序号）</param>
		/// <typeparam name="T">协议消息类型</typeparam>
		public static void Register<T>(int messageId) where T : MessageObject
		{
			if (!sr_messageDict.TryAdd(messageId, typeof(T)))
			{
				sr_messageDict.TryGetValueByKey(messageId, out var exist);
				throw new InvalidOperationException($"消息Id重复==>当前ID:{messageId},已有ID类型:{exist?.FullName ?? typeof(T).FullName}");
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
	}
}
