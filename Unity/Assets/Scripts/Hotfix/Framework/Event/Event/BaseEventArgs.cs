using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件基类。
	/// </summary>
	public abstract class BaseEventArgs : IReference
	{
		/// <summary>
		/// 获取事件ID。
		/// </summary>
		public virtual string Id => GetType().FullName;

		/// <summary>
		/// 清理引用。
		/// </summary>
		public abstract void Clear();
	}
}