using FairyGUI;
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
	/// 错误契约：全部守卫以 FuLogger 日志表达，不抛异常——监听器为空、账本无对应条目等
	/// 异常调用均记日志并忽略，调用流程不中断（失效可观测但不致命）。与 EventRegister 守卫风格对齐。
	/// 账本契约：m_uiEventListenerDic 为账本，FairyGUI EventListener 内部回调为总线。EventBridge.Add
	/// 以"先减后加"幂等去重，重复登记只会造成账本多份、总线一份的失配，故订阅侧幂等去重保证
	/// 账本与总线条目严格 1:1，移除/清理按账本逐条对称回收总线。
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
		}

		/// <summary>
		/// 清理所有注册的事件
		/// </summary>
		public void Clear() => ClearAllUIListener();

		/// <summary>
		/// 释放资源
		/// </summary>
		public void Release() => ReferencePool.Recycle(this);
	}
}