// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 推送消息宿主接口：业务模块实现它，即可用 [NotifyMessageHandler] 特性接收并处理服务器推送（Notify）消息。
	/// 说明：
	///     1. 仅服务 Notify 推送：Resp 响应由 Call 的 await 经 RPC 配对消费，不进入消息派发，故本接口与 Req/Resp 无关；
	///     2. 实现者在 Register() 中调用 ProtoMessageHandlerRegistry.Add(this) 完成注册（绑定生成器产出的零反射委托）；
	///     3. 注册期由 NotifyMessageHandlerAttribute 校验：目标消息类型必须实现 INotifyMessage。
	/// </summary>
	public interface INotifyMessageHandler
	{
		/// <summary>
		/// 注册入口：把本宿主接入消息派发（内部调 ProtoMessageHandlerRegistry.Add(this)）。
		/// </summary>
		void Register();
	}
}