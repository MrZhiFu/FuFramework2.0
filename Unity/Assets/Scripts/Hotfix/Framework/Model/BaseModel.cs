using System;
using Hotfix.Framework.Event;

namespace Hotfix.Framework.Model
{
	/// <summary>
	/// Model基类。
	/// 功能：
	///     1. 提供Model事件的注册，广播功能。
	///     2. 定义Model生命周期的接口。
	/// </summary>
	public abstract class BaseModel
	{
		/// <summary>
		/// 事件订阅器。
		/// </summary>
		private EventRegister EventRegister { get; set; }

		/// <summary>
		/// 初始化
		/// </summary>
		internal void Init()
		{
			EventRegister = EventRegister.Create();
			try
			{
				OnInitData();
				RegisterEvents();
			}
			catch
			{
				// 初始化失败：立刻归还注册器再上抛。否则半初始化的 Model 若不再走 Dispose，
				// 这个已取出的注册器永远不回引用池（引用池计数不归零）。
				EventRegister.Release();
				EventRegister = null;
				throw;
			}
		}

		/// <summary>
		/// 释放
		/// </summary>
		internal void Dispose()
		{
			// OnDispose 是用户代码：即便抛异常也必须归还事件注册器并置空，
			// 否则注册器（及其订阅表）永久泄漏。
			try
			{
				OnDispose();
			}
			finally
			{
				EventRegister?.Release();
				EventRegister = null;
			}
		}


		/// <summary>
		/// 初始化Model数据
		/// </summary>
		protected virtual void OnInitData() { }

		/// <summary>
		/// 释放Model资源。
		/// </summary>
		protected virtual void OnDispose() { }

		/// <summary>
		/// 注册Model事件。
		/// </summary>
		protected virtual void RegisterEvents() { }


		/// <summary>
		/// 订阅事件（旧签名：并存期遗留，删除段退役，业务勿新增调用）
		/// </summary>
		/// <param name="eventId">事件Id</param>
		/// <param name="handler">事件处理方法</param>
		protected void Subscribe(string eventId, EventHandler<GameEventArgs> handler) => EventRegister?.Subscribe(eventId, handler);

		/// <summary>
		/// 订阅事件（类型键：事件参数类型即事件标识，见 BaseEventArgs.Id）
		/// </summary>
		/// <typeparam name="T">事件参数类型（事件标识）。</typeparam>
		/// <param name="handler">事件处理方法</param>
		protected void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs => EventRegister?.Subscribe<T>(handler);

		/// <summary>
		/// 取消订阅事件（旧签名：并存期遗留，删除段退役，业务勿新增调用）
		/// </summary>
		/// <param name="eventId">事件Id</param>
		/// <param name="handler">事件处理方法</param>
		protected void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler) => EventRegister?.UnSubscribe(eventId, handler);

		/// <summary>
		/// 取消订阅事件（类型键：事件参数类型即事件标识，见 BaseEventArgs.Id）
		/// </summary>
		/// <typeparam name="T">事件参数类型（事件标识）。</typeparam>
		/// <param name="handler">事件处理方法</param>
		protected void UnSubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs => EventRegister?.UnSubscribe<T>(handler);

		/// <summary>
		/// 抛出事件（旧签名：并存期遗留，删除段退役，业务勿新增调用）
		/// </summary>
		/// <param name="eventArgs">消息对象</param>
		/// <param name="sender">事件发送者。</param>
		protected void Broadcast(object sender, GameEventArgs eventArgs) => EventRegister?.Broadcast(sender, eventArgs);

		/// <summary>
		/// 抛出事件（类型键形态：无 sender），事件会在抛出后的下一帧分发。
		/// </summary>
		/// <param name="eventArgs">消息对象</param>
		public void Broadcast(GameEventArgs eventArgs) => EventRegister?.Broadcast(eventArgs);

		/// <summary>
		/// 立即抛出事件（类型键形态：无 sender），事件会立刻分发。
		/// </summary>
		/// <param name="eventArgs">事件内容。</param>
		public void BroadcastNow(GameEventArgs eventArgs) => EventRegister?.BroadcastNow(eventArgs);

		/// <summary>
		/// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
		/// </summary>
		/// <param name="eventId">事件编号。</param>
		/// <param name="sender">事件发送者。</param>
		public void Broadcast(object sender, string eventId) => EventRegister?.Broadcast(sender, eventId);

		/// <summary>
		/// 立即抛出事件，这个操作不是线程安全的，事件会立刻分发。
		/// </summary>
		/// <param name="eventArgs">事件内容。</param>
		/// <param name="sender">事件发送者。</param>
		public void BroadcastNow(object sender, GameEventArgs eventArgs) => EventRegister?.BroadcastNow(sender, eventArgs);
	}
}