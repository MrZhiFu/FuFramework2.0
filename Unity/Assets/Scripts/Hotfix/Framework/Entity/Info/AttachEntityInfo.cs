using UnityEngine;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
	/// <summary>
	/// 附加实体信息：实体附加到其他实体时暂存附加信息。
	/// </summary>
	internal sealed class AttachEntityInfo : IReference
	{
		/// <summary>
		/// 父级对象
		/// </summary>
		public Transform ParentTransform { get; private set; }

		/// <summary>
		/// 用户自定义数据
		/// </summary>
		public object UserData { get; private set; }

		/// <summary>
		/// 创建附加实体信息实例
		/// </summary>
		/// <param name="parentTransform">父级 Transform。</param>
		/// <param name="userData">用户自定义数据。</param>
		public static AttachEntityInfo Create(Transform parentTransform, object userData)
		{
			var attachEntityInfo = ReferencePool.Acquire<AttachEntityInfo>();
			attachEntityInfo.ParentTransform = parentTransform;
			attachEntityInfo.UserData        = userData;
			return attachEntityInfo;
		}

		/// <summary>
		/// 清理
		/// </summary>
		public void Clear()
		{
			ParentTransform = null;
			UserData        = null;
		}
	}
}