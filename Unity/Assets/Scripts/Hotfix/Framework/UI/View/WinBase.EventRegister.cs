using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
	/// <summary>
	/// 界面基类分部：业务事件订阅器——订阅/取消订阅/分发，随窗口生命周期统一清理。
	/// </summary>
	public abstract partial class WinBase
	{
		/// <summary>
		/// 界面事件订阅器。
		/// </summary>
		private EventRegister EventRegister { get; set; }

		/// <summary>
		/// 订阅事件（类型键：事件参数类型即事件标识，见 BaseEventArgs.Id）
		/// </summary>
		/// <typeparam name="T">事件参数类型（事件标识）。</typeparam>
		/// <param name="handler">事件处理回调</param>
		public void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
		{
			handler.NotNull(nameof(handler));
			EventRegister.NotNull("事件订阅器");
			EventRegister.Subscribe<T>(handler);
		}

		/// <summary>
		/// 取消订阅事件（类型键：事件参数类型即事件标识，见 BaseEventArgs.Id）
		/// </summary>
		/// <typeparam name="T">事件参数类型（事件标识）。</typeparam>
		/// <param name="handler">事件处理回调</param>
		public void UnSubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
		{
			handler.NotNull(nameof(handler));
			EventRegister.NotNull("事件订阅器");
			EventRegister.UnSubscribe<T>(handler);
		}

		/// <summary>
		/// 抛出事件（类型键形态：无 sender）。事件在抛出后的下一帧分发。
		/// </summary>
		/// <param name="eventArgs">事件对象</param>
		public void Broadcast(GameEventArgs eventArgs)
		{
			eventArgs.NotNull(nameof(eventArgs));
			EventRegister.NotNull("事件订阅器");
			EventRegister.Broadcast(eventArgs);
		}

		/// <summary>
		/// 立即抛出事件（类型键形态：无 sender），事件会立刻分发。
		/// </summary>
		/// <param name="eventArgs">事件内容。</param>
		public void BroadcastNow(GameEventArgs eventArgs)
		{
			eventArgs.NotNull(nameof(eventArgs));
			EventRegister.NotNull("事件订阅器");
			EventRegister.BroadcastNow(eventArgs);
		}

		/// <summary>
		/// 取消所有订阅
		/// </summary>
		public void UnSubscribeAll()
		{
			EventRegister.NotNull("事件订阅器");
			EventRegister.UnSubscribeAll();
		}

		/// <summary>
		/// 释放事件注册器
		/// </summary>
		private void ReleaseEventRegister()
		{
			EventRegister.Release();
			EventRegister = null;
		}
	}
}