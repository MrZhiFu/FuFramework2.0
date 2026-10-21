using System;
using ProtoBuf;
using System.Collections.Generic;
using Hotfix.Framework.Network;

namespace Hotfix.Game.Proto
{
	/// <summary>
	/// 背包道具定义
	/// </summary>
	[ProtoContract]
	public sealed class BagItem
	{
		/// <summary>
		/// 道具id
		/// </summary>
		[ProtoMember(1)]
		public int ItemId { get; set; }

		/// <summary>
		/// 道具数量
		/// </summary>
		[ProtoMember(2)]
		public long Count { get; set; }

	}

	/// <summary>
	/// 请求背包数据
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553610)]
	public sealed class ReqBagInfo : MessageObject, IRequestMessage<RespBagInfo>
	{
	}

	/// <summary>
	/// 背包数据响应
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553611)]
	public sealed class RespBagInfo : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 道具字典，key:道具id，value:道具数量
		/// </summary>
		[ProtoMember(1)]
		[ProtoMap(DisableMap = true)]
		public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		public int ErrorCode { get; set; }

	}

	/// <summary>
	/// 请求使用道具
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553612)]
	public sealed class ReqUseItem : MessageObject, IRequestMessage<RespUseItem>
	{
		/// <summary>
		/// 道具id
		/// </summary>
		[ProtoMember(1)]
		public int ItemId { get; set; }

		/// <summary>
		/// 使用数量
		/// </summary>
		[ProtoMember(2)]
		public long Count { get; set; }

	}

	/// <summary>
	/// 使用道具响应
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553613)]
	public sealed class RespUseItem : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 道具id
		/// </summary>
		[ProtoMember(1)]
		public int ItemId { get; set; }

		/// <summary>
		/// 使用后剩余的道具数量
		/// </summary>
		[ProtoMember(2)]
		public long Count { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		public int ErrorCode { get; set; }

	}

	/// <summary>
	/// 请求增加道具
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553614)]
	public sealed class ReqAddItem : MessageObject, IRequestMessage<RespAddItem>
	{
		/// <summary>
		/// 增加的道具，key:道具id，value:增加数量
		/// </summary>
		[ProtoMember(1)]
		[ProtoMap(DisableMap = true)]
		public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

	}

	/// <summary>
	/// 增加道具响应
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553615)]
	public sealed class RespAddItem : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 变化的道具，key:道具id，value:数量
		/// </summary>
		[ProtoMember(1)]
		[ProtoMap(DisableMap = true)]
		public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		public int ErrorCode { get; set; }

	}

	/// <summary>
	/// 请求减少道具
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553616)]
	public sealed class ReqRemoveItem : MessageObject, IRequestMessage<RespRemoveItem>
	{
		/// <summary>
		/// 减少的道具，key:道具id，value:减少数量
		/// </summary>
		[ProtoMember(1)]
		[ProtoMap(DisableMap = true)]
		public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

	}

	/// <summary>
	/// 减少道具响应
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553617)]
	public sealed class RespRemoveItem : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 变化的道具，key:道具id，value:数量
		/// </summary>
		[ProtoMember(1)]
		[ProtoMap(DisableMap = true)]
		public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		public int ErrorCode { get; set; }

	}

	/// <summary>
	/// 单个道具变化通知
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553618)]
	public sealed class NotifyBagItem : MessageObject, INotifyMessage
	{
		/// <summary>
		/// 道具id
		/// </summary>
		[ProtoMember(1)]
		public int ItemId { get; set; }

		/// <summary>
		/// 最终道具数量
		/// </summary>
		[ProtoMember(2)]
		public long Count { get; set; }

		/// <summary>
		/// 变化的值
		/// </summary>
		[ProtoMember(3)]
		public long Value { get; set; }

	}

	/// <summary>
	/// 背包数据变化通知
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(6553619)]
	public sealed class NotifyBagInfoChanged : MessageObject, INotifyMessage
	{
		/// <summary>
		/// 变化的道具，key:道具id，value:变化信息
		/// </summary>
		[ProtoMember(1)]
		[ProtoMap(DisableMap = true)]
		public Dictionary<int, NotifyBagItem> ItemDic { get; set; } = new Dictionary<int, NotifyBagItem>();

	}

}
