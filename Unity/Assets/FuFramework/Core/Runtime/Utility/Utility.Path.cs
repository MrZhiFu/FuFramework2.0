using System.IO;

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
            /// 获取规范的路径。如果路径中包含 \，则会自动替换为 /。
            /// </summary>
            /// <param name="path">需要规范的路径。</param>
            /// <returns>规范的路径。</returns>
            public static string GetRegularPath(string path) => path?.Replace('\\', '/');

            /// <summary>
            /// 获取远程格式的路径(将路径转换为带有文件读写协议的路径，如：file://)。
            /// </summary>
            /// <param name="path">原始路径。</param>
            /// <returns>远程格式路径。</returns>
            public static string GetRemotePath(string path)
            {
                var regularPath = GetRegularPath(path);
                if (regularPath == null) return null;
                return regularPath.Contains("://") ? regularPath : ("file:///" + regularPath).Replace("file:////", "file:///");
            }

            /// <summary>
            /// 移除空文件夹。
            /// </summary>
            /// <param name="directoryName">要处理的文件夹名称。</param>
            /// <returns>是否移除空文件夹成功。</returns>
            public static bool RemoveEmptyDirectory(string directoryName)
            {
                if (string.IsNullOrEmpty(directoryName)) throw new FuException("要处理的文件夹名称不能为空.");
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

                    if (subDirectoryCount                             > 0) return false;
                    if (Directory.GetFiles(directoryName, "*").Length > 0) return false;
                    Directory.Delete(directoryName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}