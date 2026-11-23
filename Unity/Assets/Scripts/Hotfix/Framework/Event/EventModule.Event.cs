using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块分部：事件节点定义。
	/// </summary>
	public sealed partial class EventModule
	{
		/// <summary>
		/// 事件定义。
		/// 功能：
		///     1. 包装事件参数（sender 随类型键订阅退场一并退役，节点只承载参数）。
		/// </summary>
		private sealed class Event : IReference
		{
			/// <summary>
			/// 事件参数
			/// </summary>
			public GameEventArgs EventArgs { get; private set; }

			/// <summary>
			/// 创建事件节点
			/// </summary>
			/// <param name="eventArgs">事件参数。</param>
				public static Event Create(GameEventArgs eventArgs)
			{
				var eventNodeNode = ReferencePool.Acquire<Event>();
				eventNodeNode.EventArgs = eventArgs;
				return eventNodeNode;
			}

			/// <summary>
			/// 释放事件节点
			/// </summary>
			public void Clear()
			{
				EventArgs = null;
			}
		}
	}
}
