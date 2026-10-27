using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 消息 ID 标记特性：标注在协议消息类上，声明该类的协议号（消息 ID）。
	///
	/// 消费者：
	///     发送侧反查ID写包头（GetIdByType）；
	///		接收侧正查类型反序列化（GetTypeById）。
	///
	/// 使用边界（重要）：
	///     本特性只应出现在 proto 生成物上（Game/AutoGen/Proto/，由 ProtoExport 输出）——
	///     生成器只扫描该目录，手写消息类标注无效；新增消息请走 proto 工具链
	///     （写 .proto → 跑 Protobuf/Proto2CsExport-All.bat）。
	///
	/// 消息ID构成：ID = (option module &lt;&lt; 16) | 序号（序号由 proto 内声明顺序决定，重排即换号）；
	/// 重复ID在 Register 时直接抛异常（启动期暴露，不等到运行中串包）。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class MessageIdAttribute : Attribute
	{
		/// <summary>
		/// 该消息类的协议号（消息 ID）。
		/// </summary>
		public int MessageId { get; }

		/// <summary>
		/// 消息 ID 标记特性
		/// </summary>
		/// <param name="messageId">消息 ID（由 proto 的 option module 与声明序号构成），不能重复</param>
		public MessageIdAttribute(int messageId) => MessageId = messageId;
	}
}