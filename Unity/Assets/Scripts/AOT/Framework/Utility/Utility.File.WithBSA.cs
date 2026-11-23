using Cysharp.Threading.Tasks;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace AOT.Framework.Core.Utility
{
	public static partial class UtilityAOT
	{
		/// <summary>
		/// 经 BetterStreamingAssets 插件读取 Android 平台 StreamingAssets 文件：存在性判断与内容读取。
		/// 插件模仿 System.IO.File/Directory 的 API 设计，低开销直接访问流式资产（Android 效率敏感场景）。
		/// 注意：所有文件名应保持小写，并避免非 ASCII 字符的使用。
		/// </summary>
		public static class FileWithBSA
		{
			/// <summary>
			/// 插件是否已初始化
			/// </summary>
			private static bool s_isInited = false;

			/// <summary>
			/// 判断文件是否存在
			/// </summary>
			/// <param name="path">文件路径</param>
				public static bool IsExists(string path)
			{
				var relativePath = Path.GetRelativeStreamingAssetsPath(path);
				if (relativePath == null) return false;
				CheckInited();
				return BetterStreamingAssets.FileExists(relativePath);
			}

			/// <summary>
			/// 读取指定路径的文件内容
			/// </summary>
			/// <param name="path">文件路径</param>
				public static byte[] ReadAllBytes(string path)
			{
				var relativePath = Path.GetRelativeStreamingAssetsPath(path);
				if (relativePath == null) return null;
				CheckInited();
				return BetterStreamingAssets.ReadAllBytes(relativePath);
			}

			/// <summary>
			/// 读取指定路径的文件内容
			/// </summary>
			/// <param name="path">文件路径</param>
				public static string ReadAllText(string path)
			{
				var relativePath = Path.GetRelativeStreamingAssetsPath(path);
				if (relativePath == null) return null;
				CheckInited();
				return BetterStreamingAssets.ReadAllText(relativePath);
			}

			/// <summary>
			/// 读取指定路径的文件内容
			/// </summary>
			/// <param name="path">文件路径</param>
				public static string[] ReadAllLines(string path)
			{
				var relativePath = Path.GetRelativeStreamingAssetsPath(path);
				if (relativePath == null) return null;
				CheckInited();
				return BetterStreamingAssets.ReadAllLines(relativePath);
			}

			/// <summary>
			/// 异步读取指定路径的文件内容
			/// </summary>
			/// <param name="path">目标文件路径。</param>
				public static async UniTask<byte[]> ReadAllBytesAsync(string path)
			{
				var relativePath = Path.GetRelativeStreamingAssetsPath(path);
				if (relativePath == null) return null;
				CheckInited();
				return await UniTask.RunOnThreadPool(() => BetterStreamingAssets.ReadAllBytes(relativePath));
			}

			/// <summary>
			/// 检查BetterStreamingAssets插件初始化
			/// </summary>
			private static void CheckInited()
			{
				if (s_isInited) return;
				BetterStreamingAssets.Initialize();
				s_isInited = true;
			}
		}
	}
}