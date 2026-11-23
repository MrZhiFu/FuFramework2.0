using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
	/// <summary>
	/// 显示时的实体额外信息：显示过程中暂存传递逻辑处理对象与用户数据。
	/// </summary>
	public sealed class ShowEntityInfoEx : IReference
	{
		/// <summary>
		/// 实体逻辑处理类型。
		/// </summary>
		public Type EntityLogicType { get; private set; }

		/// <summary>
		/// 用户数据。
		/// </summary>
		public object UserData { get; private set; }

		/// <summary>
		/// 创建实体显示额外信息。
		/// </summary>
		/// <param name="entityLogicType">实体逻辑类型。</param>
		/// <param name="userData">用户自定义数据。</param>
		public static ShowEntityInfoEx Create(Type entityLogicType, object userData)
		{
			var showEntityInfo = ReferencePool.Acquire<ShowEntityInfoEx>();
			showEntityInfo.EntityLogicType = entityLogicType;
			showEntityInfo.UserData        = userData;
			return showEntityInfo;
		}

		/// <summary>
		/// 清理实体显示信息。
		/// </summary>
		public void Clear()
		{
			EntityLogicType = null;
			UserData        = null;
		}
	}
}