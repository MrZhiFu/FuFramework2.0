using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 推送（Notify）处理器注册表。
	/// 说明（项目铁律 4：运行时杜绝反射）：
	///     「消息类型 → 处理委托」由生成脚本 Protobuf/gen-proto-registry.py 产出静态绑定代码
	///     （ProtoMessageRegistry.g.cs 的 RegisterNotifyHandlers），在 NetworkModule.OnInit 的
	///     RegisterAll 中一次性写入本表。委托为 static 闭包，直接捕获宿主的 static Instance
	///     （惰性触达：宿主实例在首次推送时才被创建），运行期零反射。
	///     业务侧只需在方法上标注 [NotifyMessageHandler]——声明即生效，无手动注册步骤；
	///     新增/修改标注方法后必须重新运行生成脚本。
	/// </summary>
	public static class ProtoMessageHandlerRegistry
	{
		/// <summary>
		/// 推送处理委托表：Key 为消息类型，Value 为该类型的全部处理委托（声明顺序）。
		/// 仅启动期 RegisterAll 写入一次、之后只读，派发在主线程——普通 Dictionary 即可。
		/// </summary>
		private static readonly Dictionary<Type, List<Action<MessageObject>>> sr_notifyHandlers = new();

		/// <summary>
		/// 绑定一条推送处理委托。由生成物 RegisterNotifyHandlers 调用（每条 [NotifyMessageHandler] 方法一条）。
		/// </summary>
		/// <param name="messageType">推送消息类型</param>
		/// <param name="handler">生成期产出的静态绑定委托</param>
		internal static void BindNotify(Type messageType, Action<MessageObject> handler)
		{
			messageType.NotNull(nameof(messageType));
			handler.NotNull(nameof(handler));

			if (!sr_notifyHandlers.TryGetValue(messageType, out var list))
			{
				list = new List<Action<MessageObject>>(4);
				sr_notifyHandlers[messageType] = list;
			}

			list.Add(handler);
		}

		/// <summary>
		/// 尝试将一条推送消息派发给已绑定的处理委托。
		/// 单个委托的异常就地隔离（记录后继续后续委托），不影响同消息的其他处理者。
		/// </summary>
		/// <param name="message">推送消息</param>
		/// <returns>是否存在绑定（无绑定返回 false，由调用方记录日志）。</returns>
		internal static bool TryDispatch(MessageObject message)
		{
			if (!sr_notifyHandlers.TryGetValue(message.GetType(), out var list)) return false;

			for (var i = 0; i < list.Count; i++)
			{
				try
				{
					list[i](message);
				}
				catch (Exception e)
				{
					FuLogger.LogFatal(e);
				}
			}

			return true;
		}
	}
}
