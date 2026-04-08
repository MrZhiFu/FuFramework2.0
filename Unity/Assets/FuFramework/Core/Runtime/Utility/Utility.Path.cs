using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 文件路径相关的实用函数。
        /// 功能：
        /// 1. 获取规范的路径。
        /// 2. 获取远程格式的路径（带有file:// 或 http:// 前缀）。
        /// 3. 移除空文件夹。
        /// </summary>
        public static class Path
        {
            /// <summary>
            /// 合并路径的StringBuilder
            /// </summary>
            private static readonly StringBuilder CombinePathSb = new();

            /// <summary>
            /// 热更新资源路径(应用程序外部资源路径存放路径)
            /// </summary>
            public static string AppHotfixResPath => GetRegularPath(UnityEngine.Application.persistentDataPath);

            /// <summary>
            /// 应用程序内部资源路径存放路径
            /// </summary>
            public static string AppResPath => GetRegularPath(UnityEngine.Application.streamingAssetsPath);

            /// <summary>
            /// 获取规范的路径。如果路径中包含 \，则会自动替换为 /。
            /// 如将"C:\test\test.txt"转化为"C:/test/test.txt"
            /// </summary>
            /// <param name="path">需要规范的路径。</param>
            /// <returns>规范的路径。</returns>
            public static string GetRegularPath(string path) => path?.Replace('\\', '/');

            /// <summary>
            /// 拼接路径，如："Assets/Resources/", "test.txt" => Assets/Resources/test.txt
            /// </summary>
            /// <param name="paths"></param>
            /// <returns></returns>
            public static string Combine(params string[] paths)
            {
                const string separatorA = "/";
                const string separatorB = "\\";

                CombinePathSb.Clear();

                for (var index = 0; index < paths.Length - 1; index++)
                {
                    var path = paths[index];
                    CombinePathSb.Append(path);
                    if (path.EndsWithFast(separatorA)   || path.EndsWithFast(separatorB)) continue;
                    if (path.StartsWithFast(separatorA) || path.StartsWithFast(separatorB)) continue;
                    CombinePathSb.Append(separatorA);
                }

                CombinePathSb.Append(paths[^1]); // ^1表示最后一个元素
                return CombinePathSb.ToString();
            }

            /// <summary>
            /// 移除空文件夹。
            /// </summary>
            /// <param name="directoryName">要处理的文件夹名称。</param>
            /// <returns>是否移除空文件夹成功。</returns>
            public static bool RemoveEmptyDirectory(string directoryName)
            {
                if (string.IsNullOrEmpty(directoryName))
                    throw new FuException("要处理的文件夹名称不能为空.");

                try
                {
                    if (!Directory.Exists(directoryName)) return false;

                    // 不使用 SearchOption.AllDirectories，以便于在可能产生异常的环境下删除尽可能多的目录
                    var subDirectoryNames = Directory.GetDirectories(directoryName, "*");
                    var subDirectoryCount = subDirectoryNames.Length;
                    foreach (var subDirectoryName in subDirectoryNames)
                    {
                        if (!RemoveEmptyDirectory(subDirectoryName)) continue;
                        subDirectoryCount--;
                    }

                    if (subDirectoryCount > 0) return false;

                    if (Directory.GetFiles(directoryName, "*").Length > 0) return false;
                    Directory.Delete(directoryName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// 是否是在StreamingAssets下的路径
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns></returns>
            public static bool IsStreamingAssetsPath(string path)
            {
                var regularPath = GetRegularPath(path);
                return regularPath.StartsWith(AppResPath);
            }

            /// <summary>
            /// 获取相对于StreamingAssets的路径
            /// </summary>
            /// <param name="path">完整路径</param>
            /// <returns>相对于StreamingAssets的路径，如：Assets/StreamingAssets/test.txt => test.txt</returns>
            public static string GetRelativeStreamingAssetsPath(string path)
            {
                var regularPath = GetRegularPath(path);
                return regularPath.StartsWith(AppResPath) ? regularPath[AppResPath.Length..] : null;
            }
        }
    }
}