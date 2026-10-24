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
	///     （MessageRegistry.g.cs 的 RegisterNotifyHandlers），在 NetworkModule.OnInit 的
	///     RegisterAll 中一次性写入本表。委托为 static 闭包，直接捕获宿主的 static Instance
	///     （惰性触达：宿主实例在首次推送时才被创建），运行期零反射。
	///     业务侧只需在方法上标注 [NotifyMessageHandler]——声明即生效，无手动注册步骤；
	///     新增/修改标注方法后必须重新运行生成脚本。
	/// 另提供推送派发阀门（挂起/恢复/清空）：用于「连接已建立但业务基线未就绪」的窗口期——
	/// 推送多为增量更新，依赖业务先建立数据基线，基线未就绪时挂起派发、就绪后按序回放。
	/// </summary>
	public static class NotifyMessageHandlerRegistry
	{
		/// <summary>
		/// 缓冲队列上限：达到后丢弃最旧一条并警告，防止业务忘记恢复导致无界积压。
		/// </summary>
		private const int MAX_BUFFERED_NOTIFY_COUNT = 1024;

		/// <summary>
		/// 推送派发挂起标志。挂起期间到达的推送进入缓冲队列（保持到达顺序），恢复时按序回放。
		/// </summary>
		private static bool s_dispatchSuspended;

		/// <summary>
		/// 推送处理委托表：Key 为消息类型，Value 为该类型的全部处理委托（声明顺序）。
		/// 仅启动期 RegisterAll 写入一次、之后只读，派发在主线程——普通 Dictionary 即可。
		/// </summary>
		private static readonly Dictionary<Type, List<Action<MessageObject>>> sr_notifyHandlerDict = new();

		/// <summary>
		/// 挂起期间的推送缓冲队列（保持到达顺序）。
		/// </summary>
		private static readonly Queue<MessageObject> sr_bufferedNotifies = new();

		/// <summary>
		/// 挂起推送派发：此后到达的推送进入缓冲队列（保持到达顺序）。
		/// 用于「连接已建立但业务基线未就绪」的窗口期（如登录流程拉取全量数据前挂起，完成后恢复）。
		/// 幂等：已挂起时重复调用无副作用。
		/// </summary>
		public static void SuspendDispatch()
		{
			s_dispatchSuspended = true;
		}

		/// <summary>
		/// 恢复推送派发并按到达顺序回放缓冲队列。
		/// 幂等：未挂起时调用无副作用。回放过程中可再次挂起（此时剩余消息留在队列）。
		/// </summary>
		public static void ResumeDispatch()
		{
			if (!s_dispatchSuspended) return;
			s_dispatchSuspended = false;

			// 按到达顺序回放；回放中若业务再次挂起（处理逻辑里调 Suspend），立即停止，剩余留在队列。
			while (sr_bufferedNotifies.Count > 0 && !s_dispatchSuspended)
			{
				DispatchInternal(sr_bufferedNotifies.Dequeue());
			}
		}

		/// <summary>
		/// 清空缓冲队列（不派发）。用于登出/切换账号时丢弃旧会话的推送，防止串号。
		/// </summary>
		public static void ClearBuffer()
		{
			sr_bufferedNotifies.Clear();
		}

		/// <summary>
		/// 绑定一条推送处理委托。由生成物 RegisterNotifyHandlers 调用（每条 [NotifyMessageHandler] 方法一条）。
		/// </summary>
		/// <param name="messageType">推送消息类型</param>
		/// <param name="handler">生成期产出的静态绑定委托</param>
		internal static void BindNotify(Type messageType, Action<MessageObject> handler)
		{
			messageType.NotNull(nameof(messageType));
			handler.NotNull(nameof(handler));

			if (!sr_notifyHandlerDict.TryGetValue(messageType, out var list))
			{
				list                              = new List<Action<MessageObject>>(4);
				sr_notifyHandlerDict[messageType] = list;
			}

			list.Add(handler);
		}

		/// <summary>
		/// 尝试将一条推送消息派发给已绑定的处理委托；挂起期间进入缓冲队列（保持到达顺序，恢复时按序回放）。
		/// 单个委托的异常就地隔离（记录后继续后续委托），不影响同消息的其他处理者。
		/// </summary>
		/// <param name="msgObj">推送消息</param>
		/// <returns>是否存在绑定（无绑定返回 false，由调用方记录日志）。</returns>
		internal static bool TryDispatch(MessageObject msgObj)
		{
			if (!sr_notifyHandlerDict.TryGetValue(msgObj.GetType(), out _)) return false;

			if (s_dispatchSuspended)
			{
				if (sr_bufferedNotifies.Count >= MAX_BUFFERED_NOTIFY_COUNT)
				{
					// 丢弃最旧一条：无界积压（内存）比丢推送更危险，丢弃时有警告可查。
					FuLogger.LogWarning($"推送缓冲队列已满({MAX_BUFFERED_NOTIFY_COUNT})，丢弃最旧一条。请检查是否忘记恢复推送派发。");
					sr_bufferedNotifies.Dequeue();
				}

				sr_bufferedNotifies.Enqueue(msgObj);
				return true;
			}

			DispatchInternal(msgObj);
			return true;
		}

		/// <summary>
		/// 派发单条消息到全部绑定委托（单个委托异常就地隔离）。
		/// </summary>
		private static void DispatchInternal(MessageObject msgObj)
		{
			var list = sr_notifyHandlerDict[msgObj.GetType()];
			foreach (var handler in list)
			{
				try
				{
					handler(msgObj);
				}
				catch (Exception e)
				{
					FuLogger.LogFatal(e);
				}
			}
		}
	}
}