using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// 热更新编辑器帮助类。
	/// 功能：
	///     1. 复制热更新代码DLL到Assets/Bundles/Code目录。
	///     2. 复制AOT代码DLL到Assets/Bundles/AOTCode目录。
	/// </summary>
	public static class BuildHotfixHelper
	{
		// Unity代码生成dll位置
		private const string HOT_FIX_ASSEMBLIES_DIR = "Library/ScriptAssemblies";

		// 热更DLL名称数组
		private static readonly string[] HotfixDlls = { "Hotfix.dll" };

		// 热更代码存放位置
		private const string CODE_DIR    = "Assets/Bundles/Code/";
		private const string AOTCodeDir = "Assets/Bundles/AOTCode/";

		/// <summary>
		/// 复制热更新代码Dll到Assets/Bundles/Code目录
		/// </summary>
		[MenuItem("FuFramework/Build/Copy Hotfix Code(复制热更新代码DLL到Assets>Bundles>Code)", false, FuMenuPriority.BUILD_HOTFIX_COPY_HOTFIX_CODE)]
		public static void CopyHotfixCode()
		{
			if (!Directory.Exists(CODE_DIR))
			{
				Directory.CreateDirectory(CODE_DIR);
			}

			foreach (var hotfix in HotfixDlls)
			{
				// 源DLL相对路径，相对于Unity工程根目录。Unity编辑器运行时，当前工作目录自动设置为项目根目录。
				var srcRelativePath = Path.Combine(HOT_FIX_ASSEMBLIES_DIR, hotfix);
				File.Copy(srcRelativePath, Path.Combine(CODE_DIR,        $"{hotfix}.bytes"), true);
				Debug.Log($"复制热更代码DLL--{srcRelativePath}到{CODE_DIR}完成");
			}

			AssetDatabase.Refresh();
		}

		/// <summary>
		/// 复制AOT代码DLL到Assets/Bundles/AOTCode目录。
		/// "AssembliesPostIl2CppStrip": IL2CPP裁剪后的AOT程序集目录
		/// </summary>
		[MenuItem("FuFramework/Build/Copy AOT Code(复制AOT代码DLL到Assets>Bundles>AOTCode)", false, FuMenuPriority.BUILD_HOTFIX_COPY_AOT_CODE)]
		public static void CopyAOTCode()
		{
			if (!Directory.Exists(AOTCodeDir))
			{
				Directory.CreateDirectory(AOTCodeDir);
			}

			var directoryInfo = new DirectoryInfo(Application.dataPath);
			if (directoryInfo.Parent != null)
			{
				var path = Path.Combine(directoryInfo.Parent.FullName, "HybridCLRData", "AssembliesPostIl2CppStrip", EditorUserBuildSettings.activeBuildTarget.ToString());

				var aotCodeDir    = new DirectoryInfo(path);
				var files         = aotCodeDir.GetFiles("*.dll");
				var stringBuilder = new StringBuilder();
				foreach (var fileInfo in files)
				{
					stringBuilder.AppendLine(fileInfo.Name);
					fileInfo.CopyTo(AOTCodeDir + "/" + $"{fileInfo.Name}.bytes", true);
				}

				Debug.Log(stringBuilder);
			}

			Debug.Log($"复制AOT DLL到{CODE_DIR}完成");
			AssetDatabase.Refresh();
		}
	}
}