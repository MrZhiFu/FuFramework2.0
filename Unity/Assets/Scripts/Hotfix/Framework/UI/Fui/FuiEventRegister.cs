using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
	/// <summary>
	/// FUI事件注册器
	/// 目标：用于管理FUI组件的UI响应事件。
	/// 功能：
	///     1. 添加UI上指定组件的监听事件。
	///     2. 设置UI上指定组件的监听事件(会删除以前添加的事件)。
	///     3. 移除UI上指定组件的监听事件。
	///     4. 支持异步(UniTask)回调：处理器签名直接写 async UniTask，由本注册器包装成同步回调
	///        挂上 FairyGUI 总线，统一吞 OperationCanceledException、其余异常记日志。取消契约：
	///        取消由处理器内部 await 点携带 Token 实现（网络/资源类 API 按铁律必传 Token），
	///        窗口关闭时业务于自身 await 点原生取消，本层统一静默 OCE——详见 FuiAsyncBinding。
	/// 错误契约：全部守卫以 FuLogger 日志表达，不抛异常——监听器为空、账本无对应条目等
	/// 异常调用均记日志并忽略，调用流程不中断（失效可观测但不致命）。与 EventRegister 守卫风格对齐。
	/// 账本契约：m_uiEventListenerDic 为账本，FairyGUI EventListener 内部回调为总线。EventBridge.Add
	/// 以"先减后加"幂等去重，重复登记只会造成账本多份、总线一份的失配，故订阅侧幂等去重保证
	/// 账本与总线条目严格 1:1，移除/清理按账本逐条对称回收总线。
	/// 异步绑定契约：异步回调经 m_uiAsyncBindingDic 记录"监听器 → 原始委托绑定"，绑定内缓存包装后
	/// 的同步回调（与账本共用同一委托实例）。移除/清理时按原始委托值语义（Target+Method）查绑定，
	/// 再以缓存回调对称回收账本与总线——FairyGUI 无异步委托概念，原始委托与包装回调的换算全在此收口。
	/// </summary>
	public class FuiEventRegister : IReference
	{
		/// <summary>
		/// 记录UI响应事件的多值字典(账本), key: 可相应UI元素，如按钮，value：该UI元素上的可响应事件回调。
		/// 与 EventRegister 的 m_eventHandlerDict 同构，共用 FuMultiDictionary：链表节点带缓存，
		/// 寄存器经引用池复用后，开关窗口的登记/退订/清理全程零堆分配。
		/// </summary>
		private readonly FuMultiDictionary<EventListener, EventCallback1> m_uiEventListenerDic = new();

		/// <summary>
		/// 异步回调绑定表(账本): key: 被监听者，value：该监听器上的异步绑定（原始委托 ↔ 包装回调的换算记录）。
		/// 账本 m_uiEventListenerDic 统一存包装后的同步回调，FairyGUI 无法感知异步存在；
		/// 本表按原始委托值语义匹配，是异步回调移除/幂等判重的唯一依据，与账本条目严格 1:1。
		/// </summary>
		private readonly FuMultiDictionary<EventListener, FuiAsyncBinding> m_uiAsyncBindingDic = new();

		/// <summary>
		/// 创建FUI事件注册器
		/// </summary>
		/// <returns></returns>
		public static FuiEventRegister Create() => ReferencePool.Acquire<FuiEventRegister>();

		/// <summary>
		/// 添加UI上指定组件的监听事件
		/// 幂等去重：同一 (监听器, 回调) 重复登记警告+忽略——FairyGUI EventBridge.Add 内部以
		/// "先减后加"去重，重复登记只会造成账本多份、总线一份的失配，去重保证账本与总线
		/// 条目严格 1:1，移除/清理按账本回收总线时才严格对称。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">回调函数</param>
		public void AddUIListener(EventListener listener, EventCallback1 callback)
		{
			if (listener == null)
			{
				FuLogger.LogError("添加FUI监听事件失败, 监听器为空");
				return;
			}

			if (m_uiEventListenerDic.Contains(listener, callback))
			{
				FuLogger.LogWarning($"重复登记同一UI监听回调，本次已忽略（保持账本与 FairyGUI 总线条目 1:1）:{callback}");
				return;
			}

			m_uiEventListenerDic.Add(listener, callback);
			listener.Add(callback);
		}

		/// <summary>
		/// 添加UI上指定组件的异步监听事件（带事件上下文签名）。
		/// 处理器直接写 async UniTask，包装细节见 FuiAsyncBinding：统一吞 OperationCanceledException、
		/// 其余异常记日志；取消由处理器内部 await 点携带 Token 实现。幂等去重语义与同步重载一致。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		/// <param name="cancellation">调用方生命周期取消源（如窗口的 m_cancellation）：处理器内部
		/// await 点应携带其 Token，窗口关闭时业务原生取消；窗口复用期间令牌随 Recreate 更换，
		/// 故必须传取消源实例而非令牌值</param>
		public void AddUIListener(EventListener listener, Func<EventContext, UniTask> callback, LifecycleCancellationSource cancellation)
		{
			if (!GuardAsync(listener, callback, cancellation)) return;

			var binding = new FuiAsyncBinding(callback, cancellation);
			if (m_uiAsyncBindingDic.Contains(listener, binding))
			{
				FuLogger.LogWarning($"重复登记同一UI异步监听回调，本次已忽略:{callback}");
				return;
			}

			m_uiAsyncBindingDic.Add(listener, binding);
			m_uiEventListenerDic.Add(listener, binding.Callback);
			listener.Add(binding.Callback);
		}

		/// <summary>
		/// 添加UI上指定组件的异步监听事件（无事件上下文签名）。
		/// 语义与带上下文重载一致，处理器不使用 EventContext 时用本重载更简洁。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		/// <param name="cancellation">调用方生命周期取消源</param>
		public void AddUIListener(EventListener listener, Func<UniTask> callback, LifecycleCancellationSource cancellation)
		{
			if (!GuardAsync(listener, callback, cancellation)) return;

			var binding = new FuiAsyncBinding(callback, cancellation);
			if (m_uiAsyncBindingDic.Contains(listener, binding))
			{
				FuLogger.LogWarning($"重复登记同一UI异步监听回调，本次已忽略:{callback}");
				return;
			}

			m_uiAsyncBindingDic.Add(listener, binding);
			m_uiEventListenerDic.Add(listener, binding.Callback);
			listener.Add(binding.Callback);
		}

		/// <summary>
		/// 异步重载公共守卫：任一参数为空记日志并忽略，与同步重载守卫口径一致。
		/// </summary>
		/// <returns>是否通过守卫</returns>
		private bool GuardAsync(EventListener listener, Delegate callback, LifecycleCancellationSource cancellation)
		{
			if (listener == null)
			{
				FuLogger.LogError("添加FUI监听事件失败, 监听器为空");
				return false;
			}

			if (callback == null)
			{
				FuLogger.LogError("添加FUI监听事件失败, 回调为空");
				return false;
			}

			if (cancellation == null)
			{
				FuLogger.LogError("添加FUI异步监听事件失败, 生命周期取消源为空（异步回调必须有生命周期所有者）");
				return false;
			}

			return true;
		}

		/// <summary>
		/// 移除UI上指定组件的监听事件
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">回调函数</param>
		public void RemoveUIListener(EventListener listener, EventCallback1 callback)
		{
			if (listener == null)
			{
				FuLogger.LogError("移除FUI监听事件失败, 监听器为空");
				return;
			}

			// 账本无对应条目即异常调用（未登记或已被清理）：记日志并忽略，不同步总线
			if (!m_uiEventListenerDic.Remove(listener, callback))
			{
				FuLogger.LogError($"移除FUI监听事件失败, 监听器 {listener} 上不存在登记回调 {callback}");
				return;
			}

			listener.Remove(callback);
		}

		/// <summary>
		/// 移除UI上指定组件的异步监听事件（带事件上下文签名）。
		/// 按原始委托值语义（Target+Method）匹配绑定，再以绑定的缓存回调对称回收账本与总线。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">登记时的异步回调函数</param>
		public void RemoveUIListener(EventListener listener, Func<EventContext, UniTask> callback)
		{
			if (listener == null)
			{
				FuLogger.LogError("移除FUI监听事件失败, 监听器为空");
				return;
			}

			if (callback == null)
			{
				FuLogger.LogError("移除FUI监听事件失败, 回调为空");
				return;
			}

			if (!TryRemoveAsyncBinding(listener, callback))
			{
				FuLogger.LogError($"移除FUI监听事件失败, 监听器 {listener} 上不存在登记的异步回调 {callback}");
			}
		}

		/// <summary>
		/// 移除UI上指定组件的异步监听事件（无事件上下文签名）。
		/// 语义与带上下文重载一致。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">登记时的异步回调函数</param>
		public void RemoveUIListener(EventListener listener, Func<UniTask> callback)
		{
			if (listener == null)
			{
				FuLogger.LogError("移除FUI监听事件失败, 监听器为空");
				return;
			}

			if (callback == null)
			{
				FuLogger.LogError("移除FUI监听事件失败, 回调为空");
				return;
			}

			if (!TryRemoveAsyncBinding(listener, callback))
			{
				FuLogger.LogError($"移除FUI监听事件失败, 监听器 {listener} 上不存在登记的异步回调 {callback}");
			}
		}

		/// <summary>
		/// 按原始委托值语义查找并对称回收一个异步绑定：绑定表(原始委托)与账本(包装回调)同步摘除。
		/// </summary>
		/// <param name="listener">被监听者</param>
		/// <param name="source">登记时的原始委托</param>
		/// <returns>是否找到并回收</returns>
		private bool TryRemoveAsyncBinding(EventListener listener, Delegate source)
		{
			if (!m_uiAsyncBindingDic.TryGetValue(listener, out var range)) return false;

			FuiAsyncBinding matched = null;
			foreach (var binding in range)
			{
				if (binding.MatchSource(source))
				{
					matched = binding;
					break;
				}
			}

			if (matched == null) return false;

			m_uiEventListenerDic.Remove(listener, matched.Callback);
			listener.Remove(matched.Callback);
			m_uiAsyncBindingDic.Remove(listener, matched);
			return true;
		}

		/// <summary>
		/// 设置UI上指定组件的监听事件(会删除以前添加的事件)
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">回调函数</param>
		public void SetUIListener(EventListener listener, EventCallback1 callback)
		{
			if (listener == null)
			{
				FuLogger.LogError("设置FUI监听事件失败, 监听器为空");
				return;
			}

			// 先按账本对称回收总线上的旧回调（连键删除，旧区间整体让位），再登记新回调
			if (m_uiEventListenerDic.TryGetValue(listener, out var oldRange))
			{
				foreach (var oldCallback in oldRange)
				{
					listener.Remove(oldCallback);
				}

				m_uiEventListenerDic.RemoveAll(listener);
			}

			m_uiEventListenerDic.Add(listener, callback);
			listener.Add(callback);
		}

		/// <summary>
		/// 设置UI上指定组件的异步监听事件(会删除以前添加的事件，含同步与异步回调)。
		/// 整监听器对称回收后登记新异步绑定，不做幂等去重（与同步 Set 口径一致）。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		/// <param name="cancellation">调用方生命周期取消源</param>
		public void SetUIListener(EventListener listener, Func<EventContext, UniTask> callback, LifecycleCancellationSource cancellation)
		{
			if (!GuardAsync(listener, callback, cancellation)) return;

			RemoveAllListenerBindings(listener);

			var binding = new FuiAsyncBinding(callback, cancellation);
			m_uiAsyncBindingDic.Add(listener, binding);
			m_uiEventListenerDic.Add(listener, binding.Callback);
			listener.Add(binding.Callback);
		}

		/// <summary>
		/// 设置UI上指定组件的异步监听事件(无事件上下文签名)。
		/// 语义与带上下文重载一致。
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		/// <param name="callback">异步回调函数</param>
		/// <param name="cancellation">调用方生命周期取消源</param>
		public void SetUIListener(EventListener listener, Func<UniTask> callback, LifecycleCancellationSource cancellation)
		{
			if (!GuardAsync(listener, callback, cancellation)) return;

			RemoveAllListenerBindings(listener);

			var binding = new FuiAsyncBinding(callback, cancellation);
			m_uiAsyncBindingDic.Add(listener, binding);
			m_uiEventListenerDic.Add(listener, binding.Callback);
			listener.Add(binding.Callback);
		}

		/// <summary>
		/// 对称回收指定监听器上的全部回调（同步+异步）：总线逐条摘除、账本与绑定表整键清除。
		/// </summary>
		/// <param name="listener">被监听者</param>
		private void RemoveAllListenerBindings(EventListener listener)
		{
			if (m_uiEventListenerDic.TryGetValue(listener, out var oldRange))
			{
				foreach (var oldCallback in oldRange)
				{
					listener.Remove(oldCallback);
				}

				m_uiEventListenerDic.RemoveAll(listener);
			}

			m_uiAsyncBindingDic.RemoveAll(listener);
		}

		/// <summary>
		/// 清理UI上指定组件的所有监听事件
		/// </summary>
		/// <param name="listener">被监听者(一般是交互组件，如Button)</param>
		public void ClearUIListener(EventListener listener)
		{
			if (listener == null)
			{
				FuLogger.LogError("清理FUI监听事件失败, 监听器为空");
				return;
			}

			if (!m_uiEventListenerDic.TryGetValue(listener, out var range))
			{
				FuLogger.LogError($"清理FUI监听事件失败, 监听器 {listener} 不存在");
				return;
			}

			foreach (var callback in range)
			{
				listener.Remove(callback);
			}

			m_uiEventListenerDic.RemoveAll(listener);
			m_uiAsyncBindingDic.RemoveAll(listener);
		}

		/// <summary>
		/// 清理所有UI上组件的所有监听事件
		/// </summary>
		public void ClearAllUIListener()
		{
			// 遍历期间只回收总线、不动账本（FairyGUI 侧移除不影响本字典），最后统一清空账本；
			// 边遍历边 RemoveAll 会破坏底层 Dictionary 迭代，故不做逐键委托
			foreach (var (listener, range) in m_uiEventListenerDic)
			{
				foreach (var callback in range)
				{
					listener.Remove(callback);
				}
			}

			m_uiEventListenerDic.Clear();
			m_uiAsyncBindingDic.Clear();
		}

		/// <summary>
		/// 清理所有注册的事件
		/// </summary>
		public void Clear() => ClearAllUIListener();

		/// <summary>
		/// 释放资源
		/// </summary>
		public void Release() => ReferencePool.Recycle(this);

		/// <summary>
		/// 异步回调绑定：原始异步委托 ↔ FairyGUI 同步回调的换算记录。
		/// 职责：
		///     1. 把 async UniTask 处理器包装成 EventCallback1 挂上 FairyGUI 总线（缓存同一委托实例，
		///        账本/绑定表/总线三处共用，保证移除时严格对称）。
		///     2. 统一异常契约：OperationCanceledException 静默吞（生命周期约定），其余异常记日志——
		///        业务处理器内无需自行 try/catch。
		/// 取消契约：本层不制造取消（AttachExternalCancellation 只能切断最外层 await，业务子任务链
		/// 作为孤儿照跑，且已取消令牌下业务同步段异常会被 task.Forget() 吞入未观察通道——实测确认）。
		/// 真正的取消由业务处理器内部 await 点携带 Token 实现：网络/资源类 API 按铁律必传 Token，
		/// 延时类（UniTask.Delay 等）须显式传 Token——窗口关闭（_OnClose → Cancel）时业务于自身
		/// await 点原生取消，OCE 沿链传播至此统一静默。m_cancellation 当前仅承载该契约的语义与
		/// 未来扩展位（处理器可通过它感知取消源）。
		/// 相等语义：按原始委托值语义（Target+Method）匹配——C# 实例方法组每次转换都产生新委托
		/// 实例，Remove/幂等判重传入的委托与登记时的实例不同，必须按值匹配。
		/// </summary>
		private sealed class FuiAsyncBinding
		{
			/// <summary>
			/// 登记时的原始异步委托（仅用于值语义匹配，不作调用）。
			/// </summary>
			private readonly Delegate m_source;

			/// <summary>
			/// 统一为带上下文形态的处理器（无上下文重载经一层闭包适配）。
			/// </summary>
			private readonly Func<EventContext, UniTask> m_handler;

			/// <summary>
			/// 调用方生命周期取消源：业务处理器内部 await 点应携带 Token（见类注释取消契约），
			/// 窗口复用期间 Token 随 Recreate 更换，故捕获取消源实例而非令牌值。
			/// </summary>
			private readonly LifecycleCancellationSource m_cancellation;

			/// <summary>
			/// 包装后的同步回调（挂总线、记账本，全生命周期唯一实例）。
			/// </summary>
			public EventCallback1 Callback { get; }

			/// <summary>
			/// 创建带事件上下文签名的异步绑定。
			/// </summary>
			/// <param name="handler">异步处理器</param>
			/// <param name="cancellation">调用方生命周期取消源</param>
			public FuiAsyncBinding(Func<EventContext, UniTask> handler, LifecycleCancellationSource cancellation)
			{
				m_source       = handler;
				m_handler      = handler;
				m_cancellation = cancellation;
				Callback       = Invoke;
			}

			/// <summary>
			/// 创建无事件上下文签名的异步绑定。
			/// </summary>
			/// <param name="handler">异步处理器</param>
			/// <param name="cancellation">调用方生命周期取消源</param>
			public FuiAsyncBinding(Func<UniTask> handler, LifecycleCancellationSource cancellation)
			{
				m_source       = handler;
				m_handler      = _ => handler();
				m_cancellation = cancellation;
				Callback       = Invoke;
			}

			/// <summary>
			/// 判断绑定是否源自指定原始委托（值语义：Target+Method 相同即匹配）。
			/// </summary>
			/// <param name="source">待匹配的原始委托</param>
			/// <returns>是否匹配</returns>
			public bool MatchSource(Delegate source) => m_source.Equals(source);

			/// <summary>
			/// 完整等待异步处理器，统一消化取消与异常。
			/// UniTaskVoid + Forget 为框架内部实现细节：await 全程被 try/catch 消化，
			/// 异常不会进入全局未观察通道，业务侧无需感知。
			/// </summary>
			/// <param name="context">FairyGUI 事件上下文</param>
			private async UniTaskVoid InvokeCore(EventContext context)
			{
				try
				{
					await m_handler(context);
				}
				catch (OperationCanceledException)
				{
					// 生命周期取消（业务 await 点携带 Token，窗口关闭/销毁/复用重建时触发）：按契约静默返回
				}
				catch (Exception e)
				{
					FuLogger.LogError($"FUI异步事件回调异常: {e}");
				}
			}

			/// <summary>
			/// FairyGUI 总线入口（同步）：转交异步核心后立即返回，不阻塞事件派发。
			/// </summary>
			/// <param name="context">FairyGUI 事件上下文</param>
			private void Invoke(EventContext context) => InvokeCore(context).Forget();
		}
	}
}