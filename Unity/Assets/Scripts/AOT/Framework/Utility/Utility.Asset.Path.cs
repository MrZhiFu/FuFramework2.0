// ReSharper disable once CheckNamespace

namespace AOT.Framework.Core.Utility
{
	public static partial class UtilityAOT
	{
		/// <summary>
		/// Bundle资源路径相关的实用函数集。
		/// 功能：
		///     1. Bundle资源目录定义。
		///     2. Bundle资源路径拼接。
		/// </summary>
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public static class AssetPath
		{
			/// <summary>
			/// 打包资源根路径
			/// </summary>
			public const string BUNDLES_PATH = "Assets/Bundles";

			/// <summary>
			/// 打包资源文件夹名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_NAME = "Bundles";

			/// <summary>
			/// 打包资源文件夹UI名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_UI_NAME = "UI";

			/// <summary>
			/// 打包资源文件夹Scene名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_SCENE_NAME = "Scene";

			/// <summary>
			/// 打包资源文件夹Localization名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_LOCALIZATION_NAME_PLACEHOLDER = "Localization";

			/// <summary>
			/// 打包资源文件夹Config名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_CONFIG_NAME = "Config";

			/// <summary>
			/// 打包资源文件夹AOTCode名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_AOT_CODE_NAME = "AOTCode";

			/// <summary>
			/// 打包资源文件夹Code名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_CODE_NAME = "Code";

			/// <summary>
			/// 打包资源文件夹Sound名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_SOUND_NAME = "Sound";

			/// <summary>
			/// 打包资源文件夹Prefab名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_PREFAB_NAME = "Prefabs";

			/// <summary>
			/// 打包资源文件夹Video名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_VIDEO_NAME = "Video";

			/// <summary>
			/// 打包资源文件夹Image名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_IMAGE_NAME = "Image";

			/// <summary>
			/// 打包资源文件夹Sprite名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_SPRITE_NAME = "Sprite";

			/// <summary>
			/// 打包资源文件夹Shader名称
			/// </summary>
			public const string BUNDLES_DIRECTORY_SHADER_NAME = "Shader";


			/// <summary>
			/// 获取文件路径
			/// </summary>
			/// <param name="filePath">相对于Bundles的路径，不要以/开头</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetFilePath(string filePath) => $"{BUNDLES_PATH}/{filePath}";

			/// <summary>
			/// 获取图片文件路径
			/// </summary>
			/// <param name="filePath">相对于Bundles/Image的路径，不要以/开头,需要携带扩展名</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetImagePath(string filePath) => GetCategoryFilePath(BUNDLES_DIRECTORY_IMAGE_NAME, filePath);

			/// <summary>
			/// 获取视频文件路径
			/// </summary>
			/// <param name="filePath">相对于Bundles/Video的路径，不要以/开头,需要携带扩展名</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetVideoPath(string filePath) => GetCategoryFilePath(BUNDLES_DIRECTORY_VIDEO_NAME, filePath);

			/// <summary>
			/// 获取Sprite文件路径
			/// </summary>
			/// <param name="filePath">相对于Bundles/Sprite的路径，不要以/开头,需要携带扩展名</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetSpritePath(string filePath) => GetCategoryFilePath(BUNDLES_DIRECTORY_SPRITE_NAME, filePath);

			/// <summary>
			/// 获取Prefab文件路径
			/// </summary>
			/// <param name="filePath">相对于Bundles/Prefabs的路径，不要以/开头,需要携带扩展名</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetPrefabPath(string filePath) => GetCategoryFilePath(BUNDLES_DIRECTORY_PREFAB_NAME, filePath);

			/// <summary>
			/// 获取Prefab文件路径
			/// </summary>
			/// <param name="filePath">相对于Bundles/Prefabs的路径，不要以/开头,需要携带扩展名</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetShaderPath(string filePath) => GetCategoryFilePath(BUNDLES_DIRECTORY_SHADER_NAME, filePath);

			/// <summary>
			/// 获取配置文件路径
			/// </summary>
			/// <param name="fileName">相对于Bundles/Config的路径，不要以/开头,需要携带扩展名</param>
			/// <param name="extension">文件扩展名称</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetConfigPath(string fileName, string extension = ".bytes") =>
				GetCategoryFilePath(BUNDLES_DIRECTORY_CONFIG_NAME, $"{fileName}{extension}");

			/// <summary>
			/// 获取AOT元数据代码文件路径
			/// </summary>
			/// <param name="fileName">相对于Bundles/AOTCode的路径，不要以/开头,需要携带扩展名</param>
			/// <param name="extension">文件扩展名称</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetAOTCodePath(string fileName, string extension = ".bytes") =>
				GetCategoryFilePath(BUNDLES_DIRECTORY_AOT_CODE_NAME, $"{fileName}{extension}");

			/// <summary>
			/// 获取代码文件路径
			/// </summary>
			/// <param name="fileName">相对于Bundles/Code的路径，不要以/开头,需要携带扩展名</param>
			/// <param name="extension">文件扩展名称</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetCodePath(string fileName, string extension = ".bytes") =>
				GetCategoryFilePath(BUNDLES_DIRECTORY_CODE_NAME, $"{fileName}{extension}");

			/// <summary>
			/// 获取UI文件路径
			/// </summary>
			/// <param name="uiPackageName">UI包名</param>
			/// <returns>返回拼接好的路径：Assets/Bundles/UI/{uiPackageName}/{uiPackageName}</returns>
			public static string GetUIPackagePath(string uiPackageName) =>
				GetCategoryFilePath(BUNDLES_DIRECTORY_UI_NAME, $"{uiPackageName}/{uiPackageName}");

			/// <summary>
			/// 获取UI文件路径
			/// </summary>
			/// <returns>返回拼接好的路径: Assets/Bundles/UI/</returns>
			public static string GetUIRootPath() => $"{BUNDLES_PATH}/{BUNDLES_DIRECTORY_UI_NAME}/";

			/// <summary>
			/// 获取UI文件路径
			/// </summary>
			/// <param name="uiPath">UI路径</param>
			/// <returns>返回拼接好的路径: Assets/Bundles/UI/{uiPath}</returns>
			public static string GetUIPath(string uiPath) => GetCategoryFilePath(BUNDLES_DIRECTORY_UI_NAME, uiPath);

			/// <summary>
			/// 获取声音文件路径
			/// </summary>
			/// <param name="pathName">路径包含名称</param>
			/// <param name="extension">扩展名称,默认为.mp3</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetSoundPath(string pathName, string extension = ".mp3")
			{
				if (pathName.IndexOf('.') >= 0)
					return GetCategoryFilePath(BUNDLES_DIRECTORY_SOUND_NAME, pathName);

				return GetCategoryFilePath(BUNDLES_DIRECTORY_SOUND_NAME, $"{pathName}{extension}");
			}

			/// <summary>
			/// 获取场景文件路径
			/// </summary>
			/// <param name="pathName">路径包含名称</param>
			/// <param name="extension">扩展名,默认为.unity</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetScenePath(string pathName, string extension = ".unity")
			{
				if (pathName.IndexOf('.') >= 0)
					return GetCategoryFilePath(BUNDLES_DIRECTORY_SCENE_NAME, pathName);

				return GetCategoryFilePath(BUNDLES_DIRECTORY_SCENE_NAME, $"{pathName}{extension}");
			}

			/// <summary>
			/// 获取本地化文件路径
			/// </summary>
			/// <param name="pathName">路径包含名称</param>
			/// <param name="extension">文件扩展名</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetLocalizationPath(string pathName, string extension = ".xml")
			{
				if (pathName.IndexOf('.') >= 0)
					return GetCategoryFilePath(BUNDLES_DIRECTORY_LOCALIZATION_NAME_PLACEHOLDER, pathName);

				return GetCategoryFilePath(BUNDLES_DIRECTORY_LOCALIZATION_NAME_PLACEHOLDER, $"{pathName}{extension}");
			}

			/// <summary>
			/// 获取根据类别文件夹名称和文件路径获得完整文件路径
			/// </summary>
			/// <param name="category">相对于Bundles的类别名称</param>
			/// <param name="filePath">相对于Bundles的路径，不要以/开头</param>
			/// <returns>返回拼接好的路径</returns>
			public static string GetCategoryFilePath(string category, string filePath) => $"{BUNDLES_PATH}/{category}/{filePath}";
		}
	}
}