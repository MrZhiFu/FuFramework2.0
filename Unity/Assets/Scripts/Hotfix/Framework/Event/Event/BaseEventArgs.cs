using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件基类。
	/// </summary>
	public abstract class BaseEventArgs : EventArgs, IReference
	{
		/// <summary>
		/// 获取事件ID。
		/// </summary>
		public virtual string Id => GetType().FullName;   // 基类统一派生：具体事件类的 FullName；EmptyEventArgs 仍 override（并存期 string 重载使用）

		/// <summary>
		/// 清理引用。
		/// </summary>
		public abstract void Clear();
	}
}