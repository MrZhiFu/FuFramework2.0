using System;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using Hotfix.Framework.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
	/// <summary>
	/// 界面实例对象（对象池单元）：负责界面实例的创建与销毁。
	/// </summary>
	public sealed class WinObject : ObjectBase
	{
		/// <summary>
		/// 创建界面实例对象。
		/// </summary>
		/// <param name="winName">界面名称。</param>
		/// <param name="winBase">界面基类实例。</param>
		/// <returns>创建的界面实例对象。</returns>
		public static WinObject Create(string winName, WinBase winBase)
		{
			var winObject = ReferencePool.Acquire<WinObject>();
			winObject.Initialize(winName, winBase);
			return winObject;
		}

		/// <summary>
		/// 销毁界面实例对象
		/// </summary>
		protected internal override void OnDispose()
		{
			if (Target is not WinBase winBase)
			{
				FuLogger.LogError($"界面实例对象 '{Name}' 的目标对象不是界面基类 WinBase，已忽略本次销毁");
				return;
			}

			try
			{
				winBase.WinUI?.Dispose();
			}
			catch (Exception e)
			{
				FuLogger.LogWarning($"销毁 WinUI 时出现异常: {e.Message}");
			}

			winBase._OnDispose();
		}
	}
}