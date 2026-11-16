using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
	/// <summary>
	/// 界面基类分部类之一。
	/// 目标：提供一个UI事件订阅器，用于管理界面的UI事件。
	/// 功能：
	///     1. 添加UI上指定组件的监听事件。
	///     2. 设置UI上指定组件的监听事件(会删除以前添加的事件)。
	///     3. 移除UI上指定组件的监听事件。
	/// </summary>
	public abstract partial class WinBase
	{
		/// <summary>
		/// UI事件订阅器
		/// </summary>
		private FuiEventRegister UIEventRegister { get; set; }

		/// <summary>
		/// 添加UI上指定组件的监听事件
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">回调函数</param>
		public void AddUIListener(EventListener listener, EventCallback1 callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.AddUIListener(listener, callback);
		}

		/// <summary>
		/// 设置UI上指定组件的监听事件(会删除以前添加的事件)
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">回调函数</param>
		public void SetUIListener(EventListener listener, EventCallback1 callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.SetUIListener(listener, callback);
		}

		/// <summary>
		/// 添加UI上指定组件的异步监听事件（带事件上下文签名）。
		/// 处理器直接写 async UniTask，调用写法与同步版一致——处理器内部 await 点携带 Token
		/// （基类 protected Token 属性，网络/资源类 API 按铁律必传），窗口关闭时业务原生取消，
		/// 取消与异常均由适配层统一处理，业务处理器内无需 try/catch。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		public void AddUIListener(EventListener listener, Func<EventContext, UniTask> callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.AddUIListener(listener, callback);
		}

		/// <summary>
		/// 添加UI上指定组件的异步监听事件（无事件上下文签名）。
		/// 语义与带上下文重载一致，处理器不使用 EventContext 时用本重载更简洁。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		public void AddUIListener(EventListener listener, Func<UniTask> callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.AddUIListener(listener, callback);
		}

		/// <summary>
		/// 设置UI上指定组件的异步监听事件(会删除以前添加的事件)。
		/// 取消契约与同步 Set 的异步对应版一致（处理器 await 点携带 Token），语义与同步 Set 一致。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		public void SetUIListener(EventListener listener, Func<EventContext, UniTask> callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.SetUIListener(listener, callback);
		}

		/// <summary>
		/// 设置UI上指定组件的异步监听事件(无事件上下文签名)。
		/// 生命周期取消源由基类自动补传，语义与同步 Set 一致。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		public void SetUIListener(EventListener listener, Func<UniTask> callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.SetUIListener(listener, callback);
		}

		/// <summary>
		/// 移除UI上指定组件的监听事件
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">回调函数</param>
		public void RemoveUIListener(EventListener listener, EventCallback1 callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.RemoveUIListener(listener, callback);
		}

		/// <summary>
		/// 移除UI上指定组件的异步监听事件（带事件上下文签名）。
		/// 按原始委托值语义匹配登记绑定后对称回收账本与总线。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">登记时的异步回调函数</param>
		public void RemoveUIListener(EventListener listener, Func<EventContext, UniTask> callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.RemoveUIListener(listener, callback);
		}

		/// <summary>
		/// 移除UI上指定组件的异步监听事件（无事件上下文签名）。
		/// 语义与带上下文重载一致。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">登记时的异步回调函数</param>
		public void RemoveUIListener(EventListener listener, Func<UniTask> callback)
		{
			listener.NotNull(nameof(listener));
			callback.NotNull(nameof(callback));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.RemoveUIListener(listener, callback);
		}

		/// <summary>
		/// 清理UI上指定组件的所有监听事件
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		public void ClearUIListener(EventListener listener)
		{
			listener.NotNull(nameof(listener));

			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.ClearUIListener(listener);
		}

		/// <summary>
		/// 清理UI上所有组件的所有监听事件
		/// </summary>
		public void ClearAllUIListener()
		{
			UIEventRegister.NotNull("UI事件订阅器");
			UIEventRegister.ClearAllUIListener();
		}

		/// <summary>
		/// 释放UI事件注册器
		/// </summary>
		private void ReleaseUIEventRegister()
		{
			UIEventRegister.Release();
			UIEventRegister = null;
		}
	}
}