using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 热更程序集定义文件帮助类。
    /// 用于修改指定路径的程序集定义文件(.asmdef)，勾选/取消在排除平台中添加Editor平台，即标记/取消该程序集仅在非Editor环境(运行时)下使用。
    /// </summary>
    public static class HotFixAssemblyDefinitionHelper
    {
        /// <summary>
        /// 程序集定义文件信息
        /// </summary>
        private sealed class AssemblyDefinitionInfo
        {
            /// <summary>
            /// 名称
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// 根命名空间
            /// </summary>
            public string rootNamespace { get; set; }

            /// <summary>
            /// 引用的程序集列表
            /// </summary>
            public List<string> references { get; set; }

            /// <summary>
            /// 包含的平台列表
            /// </summary>
            public List<string> includePlatforms { get; set; }

            /// <summary>
            /// 排除的平台列表
            /// </summary>
            public List<string> excludePlatforms { get; set; }

            /// <summary>
            /// 是否允许不安全代码
            /// </summary>
            public bool allowUnsafeCode { get; set; }

            /// <summary>
            /// 重载的引用
            /// </summary>
            public bool overrideReferences { get; set; }

            /// <summary>
            /// 预编译的引用列表
            /// </summary>
            public List<string> precompiledReferences { get; set; }

            /// <summary>
            /// 自动引用
            /// </summary>
            public bool autoReferenced { get; set; }

            /// <summary>
            /// 定义约束列表
            /// </summary>
            public List<string> defineConstraints { get; set; }

            /// <summary>
            /// 版本号定义列表
            /// </summary>
            public List<string> versionDefines { get; set; }

            /// <summary>
            /// 非引擎引用列表
            /// </summary>
            public bool noEngineReferences { get; set; }
        }


        /// <summary>
        /// 修改指定路径的程序集定义文件(.asmdef)，勾选在排除平台中添加Editor平台，即标记该程序集仅在非Editor环境(运行时)下使用。
        /// </summary>
        /// <param name="path"></param>
        internal static void AddEditorInExcludePlatforms(string path)
        {
            var assemblyDefinitionAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

            var info     = JsonConvert.DeserializeObject<AssemblyDefinitionInfo>(assemblyDefinitionAsset.text);
            var isEditor = info.excludePlatforms.Any(m => m == "Editor");

            if (isEditor) return;
            info.excludePlatforms.Add("Editor");
            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(info, Formatting.Indented));
            AssetDatabase.ImportAsset(path);
        }

        /// <summary>
        /// Editor指定路径的程序集定义文件(.asmdef)，取消勾选在排除平台中的Editor平台，即标记该程序集在Editor环境下也可使用。
        /// </summary>
        /// <param name="path"></param>
        internal static void RemoveEditorInExcludePlatforms(string path)
        {
            var assemblyDefinitionAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

            var info     = JsonConvert.DeserializeObject<AssemblyDefinitionInfo>(assemblyDefinitionAsset.text);
            var isEditor = info.excludePlatforms.Any(m => m == "Editor");

            if (isEditor)
                info.excludePlatforms.Remove("Editor");

            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(info, Formatting.Indented));
            AssetDatabase.ImportAsset(path);
        }
    }
}