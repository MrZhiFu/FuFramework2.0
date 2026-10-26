using ProtoBuf;
using Newtonsoft.Json;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Utility;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 消息基类
	/// </summary>
	[ProtoContract]
	public class MessageObject
	{
		/// <summary>
		/// 消息唯一编号
		/// </summary>
		[JsonIgnore]
		public int UniqueId { get; private set; }

		/// <summary>
		/// 创建消息对象并生成唯一编号
		/// </summary>
		protected MessageObject() => UpdateUniqueId();

		/// <summary>
		/// 生成新的唯一编号（发包侧取新号供 RPC 配对；构造时自动调用，心跳等复用实例每次发送前刷新）
		/// </summary>
		public void UpdateUniqueId() => UniqueId = Utility.IdGenerator.GetNextUniqueIntId();

		/// <summary>
		/// 设置唯一编号（收包侧从包头还原请求号，供 RPC 配对）
		/// </summary>
		internal void SetUniqueId(int uniqueId) => UniqueId = uniqueId;

		/// <summary>
		/// 序列化为 JSON 字符串
		/// </summary>
		/// <returns>消息的 JSON 表示</returns>
		public override string ToString() => UtilityAOT.Json.ToJson(this);
	}
}