using System;
using Hotfix.Framework.Core;
using AOT.Framework.Extension;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 推送（Notify）消息处理方法特性（**声明用**）。
	/// 仅服务服务器推送：目标方法的消息类型必须实现 INotifyMessage（构造期校验）；
	/// Resp 响应由 Call 的 await 经 RPC 配对消费，不进入消息派发，因此 Req/Resp 类型不可标注本特性。
	/// 声明即生效：生成脚本扫描本特性产出静态绑定委托（宿主经 static Instance 捕获），
	/// 业务侧无需实现接口、无需手动注册。
	///
	/// 说明（项目铁律 4：运行时杜绝反射）：
	///     本特性仅作源码声明，供生成脚本 Protobuf/gen-proto-registry.py 扫描；
	///     运行期由生成物 MessageRegistry.gen.cs 直接产出静态绑定委托写入注册表，
	///     不读取特性、不查找方法、无任何反射调用。
	///
	/// 可见性契约：
	///     目标方法必须为 <c>internal</c> 或 <c>public</c>（生成物位于同程序集
	///     <c>Hotfix.Framework.Network</c>，需能直接调用）。若是 <c>private</c> / <c>protected</c>，
	///     生成脚本会**报错并中止**，提示改为 internal/public。
	/// </summary>
	/// <example>
	/// <code>
	/// public sealed class BagManager : Singleton&lt;BagManager&gt;
	/// {
	///     [NotifyMessageHandler(typeof(NotifyBagInfoChanged), nameof(OnNotifyBagInfoChanged))]
	///     internal void OnNotifyBagInfoChanged(NotifyBagInfoChanged msg) { /* ... */ }
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Method)]
	public class NotifyMessageHandlerAttribute : Attribute
	{
		/// <summary>
		/// [NotifyMessageHandler] 声明的消息类型。
		/// </summary>
		public Type MessageType { get; }

		/// <summary>
		/// 执行的方法名称（仅用于生成物注释与日志，不参与任何反射查找）
		/// </summary>
		public string MethodName { get; }

		/// <summary>
		/// 派发优先级：同一消息的多个处理者按数字**升序**执行（小者先，解决模块间数据依赖）。
		/// 默认 0；同优先级保持声明顺序。仅影响同消息多处理者的相对顺序，不改变消息到达顺序。
		/// </summary>
		public int Priority { get; set; }

		/// <summary>
		/// 网络消息处理器特性
		/// </summary>
		/// <param name="message">要处理的消息类型。需要继承 MessageObject 并实现 INotifyMessage</param>
		/// <param name="invokeMethodName">执行的方法名称。建议使用 nameof 标记当前的函数</param>
		public NotifyMessageHandlerAttribute(Type message, string invokeMethodName)
		{
			message.NotNull(nameof(message));
			invokeMethodName.NotNullOrEmpty(nameof(invokeMethodName));
			MethodName = invokeMethodName;
			if (message.BaseType != typeof(MessageObject))
				throw new ArgumentException("message必须继承:" + nameof(MessageObject));

			if (!message.IsImplWithInterface(typeof(INotifyMessage)))
				throw new ArgumentException($"message:{message.FullName}必须实现:" + nameof(INotifyMessage));

			MessageType = message;
		}
	}
}