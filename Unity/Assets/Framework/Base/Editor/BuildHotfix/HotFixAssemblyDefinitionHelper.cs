using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;

namespace GameFrameX.Editor
{
    public static class HotFixAssemblyDefinitionHelper
    {
        sealed class AssemblyDefinitionInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string rootNamespace { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> references { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> includePlatforms { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> excludePlatforms { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public bool allowUnsafeCode { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public bool overrideReferences { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> precompiledReferences { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public bool autoReferenced { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> defineConstraints { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> versionDefines { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public bool noEngineReferences { get; set; }
        }


        /// <summary>
        /// 为指定路径的程序集定义文件添加Editor平台
        /// </summary>
        /// <param name="path"></param>
        internal static void AddEditor(string path)
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
        /// 从指定路径的程序集定义文件移除Editor平台
        /// </summary>
        /// <param name="path"></param>
        internal static void RemoveEditor(string path)
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