using UnityEngine;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
// ReSharper disable StaticMemberInGenericType
namespace Hotfix.Framework.Core
{
	/// <summary>
	/// 游戏框架Mono单例(线程安全)
	/// </summary>
	/// <typeparam name="T">单例类型</typeparam>
	public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
	{
		/// <summary>
		/// 单例对象
		/// </summary>
		private static T s_instance;

		/// <summary>
		/// 是否已初始化--防止重复初始化
		/// </summary>
		private static bool s_isInitialized;

		/// <summary>
		/// 单例对象
		/// </summary>
		public static T Instance
		{
			get
			{
				if (s_instance != null)
				{
					return s_instance;
				}

				s_instance = FindFirstObjectByType<T>();
				if (s_instance != null)
				{
					// 确保手动放置在场景中的实例也被正确初始化
					if (!s_isInitialized)
					{
						s_instance.Init();
					}

					return s_instance;
				}

				// 创建新实例
				var singletonObject = new GameObject();
				s_instance           = singletonObject.AddComponent<T>();
				singletonObject.name = $"[Singleton] {typeof(T).Name}";

				DontDestroyOnLoad(singletonObject);
				s_instance.Init();

				return s_instance;
			}
		}

		/// <summary>
		/// Awake生命周期：处理场景中手动放置的单例组件
		/// </summary>
		private void Awake()
		{
			// 编辑器模式下跳过
			if (!Application.isPlaying) return;

			// 防止在场景中手动放置了多个单例组件而导致创建重复实例
			if (s_instance && s_instance != this)
			{
				FuLogger.LogWarning($"[MonoSingleton] 场景中已存在同类型的单例组件 '{typeof(T)}', 该单例{gameObject.name}被立即销毁!");
				DestroyImmediate(gameObject);
				return;
			}

			// 确保场景中手动放置的单例组件也被正确初始化
			if (!s_instance)
			{
				s_instance = this as T;
				DontDestroyOnLoad(gameObject);

				if (!s_isInitialized)
					Init();
			}
		}

		/// <summary>
		/// 销毁
		/// </summary>
		private void OnDestroy()
		{
			if (s_instance != this) return;
			OnDispose();
			s_instance      = null;
			s_isInitialized = false;
		}

		/// <summary>
		/// 初始化单例（确保只初始化一次）
		/// </summary>
		private void Init()
		{
			if (s_isInitialized) return;
			s_isInitialized = true;
			OnInit();
		}

		/// <summary>
		/// 初始化
		/// </summary>
		protected virtual void OnInit() { }

		/// <summary>
		/// 释放资源
		/// </summary>
		protected virtual void OnDispose() { }
	}
}