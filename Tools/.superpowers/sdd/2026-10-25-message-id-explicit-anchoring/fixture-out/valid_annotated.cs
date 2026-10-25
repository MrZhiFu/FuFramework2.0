using System;
using ProtoBuf;
using System.Collections.Generic;
using Hotfix.Framework.Network;

namespace Test.Proto
{
	/// <summary>
	/// 
	/// </summary>
	[ProtoContract]
	[MessageId(45875210)]
	public sealed class ReqPing : MessageObject, IRequestMessage<RespPing>
	{
		/// <summary>
		/// 标识
		/// </summary>
		[ProtoMember(1)]
		public int Id { get; set; }

	}

	/// <summary>
	/// 
	/// </summary>
	[ProtoContract]
	[MessageId(45875211)]
	public sealed class RespPing : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 错误码
		/// </summary>
		[ProtoMember(1)]
		public int ErrorCode { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		public int ErrorCode { get; set; }

	}

	/// <summary>
	/// 
	/// </summary>
	[ProtoContract]
	public sealed class PlayerInfo
	{
		/// <summary>
		/// 玩家ID
		/// </summary>
		[ProtoMember(1)]
		public long Id { get; set; }

	}

}
