using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class UtilityAOT
    {
        /// <summary>
        /// 文件相关的实用函数。
        /// 功能：
        ///     1.获取带有单位的字节大小。
        ///     2.获取目录下的所有文件。
        ///     3.清理目录。
        ///     4.目录复制。
        ///     5.复制文件到目标目录。
        ///     6.删除文件。
        ///     7.判断文件是否存在。  
        ///     8.判断是否是Android的只读路径。
        ///     9.移动文件到目标目录。
        ///     10.读取指定路径的文件内容。
        ///     11.写入指定路径的文件内容。
        /// </summary>
        public static class File
        {
            /// <summary>
            /// 字节大小单位列表
            /// </summary>
            private static readonly string[] UnitList = { "B", "KB", "MB", "GB", "TB", "PB" };

            /// <summary>
            /// 获取带有单位的字节大小
            /// </summary>
            /// <param name="size">字节大小</param>
            /// <returns>格式化后的字节大小字符串</returns>
            public static string GetBytesSizeWithUnit(long size)
            {
                foreach (var unit in UnitList)
                {
                    if (size <= 1024)
                    {
                        return size + unit;
                    }

                    size /= 1024;
                }

                return size + UnitList[0];
            }

            /// <summary>
            /// 获取目录下的所有文件
            /// </summary>
            /// <param name="files">文件存放路径列表对象</param>
            /// <param name="dir">目标目录</param>
            public static void GetAllFiles(List<string> files, string dir)
            {
                if (!Directory.Exists(dir)) return;

                var strings = Directory.GetFiles(dir);
                files.AddRange(strings);

                var subDirs = Directory.GetDirectories(dir);
                foreach (var subDir in subDirs)
                {
                    GetAllFiles(files, subDir);
                }
            }

            /// <summary>
            /// 清理目录
            /// </summary>
            /// <param name="dir">目标路径</param>
            public static void CleanDirectory(string dir)
            {
                if (!Directory.Exists(dir)) return;

                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    Directory.Delete(subDir, true);
                }

                foreach (var subFile in Directory.GetFiles(dir))
                {
                    System.IO.File.Delete(subFile);
                }
            }

            /// <summary>
            /// 目录复制
            /// </summary>
            /// <param name="srcDir">源路径</param>
            /// <param name="targetDir">目标路径</param>
            /// <exception cref="Exception"></exception>
            public static void CopyDirectory(string srcDir, string targetDir)
            {
                var source = new DirectoryInfo(srcDir);
                var target = new DirectoryInfo(targetDir);

                if (target.FullName.StartsWith(source.FullName, StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("父目录不能拷贝到子目录！");

                if (!source.Exists) return;
                if (!target.Exists) target.Create();

                var files = source.GetFiles();
                foreach (var file in files)
                {
                    System.IO.File.Copy(file.FullName, System.IO.Path.Combine(target.FullName, file.Name), true);
                }

                var dirs = source.GetDirectories();
                foreach (var dir in dirs)
                {
                    CopyDirectory(dir.FullName, System.IO.Path.Combine(target.FullName, dir.Name));
                }
            }

            /// <summary>
            /// 复制文件到目标目录
            /// </summary>
            /// <param name="sourceFileName">源路径</param>
            /// <param name="destFileName">目标路径</param>
            /// <param name="overwrite">是否覆盖</param>
            public static void Copy(string sourceFileName, string destFileName, bool overwrite = false)
            {
                if (!System.IO.File.Exists(sourceFileName)) return;
                System.IO.File.Copy(sourceFileName, destFileName, overwrite);
            }

            /// <summary>
            /// 删除文件
            /// </summary>
            /// <param name="path">文件路径</param>
            public static void Delete(string path)
            {
                if (!System.IO.File.Exists(path)) return;
                System.IO.File.Delete(path);
            }

            /// <summary>
            /// 删除文件夹
            /// </summary>
            /// <param name="path">文件路径</param>
            public static void DeleteDir(string path)
            {
                if (!Directory.Exists(path)) return;
                Directory.Delete(path, true);
            }

            /// <summary>
            /// 判断文件是否存在
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <returns></returns>
            public static bool IsExists(string path)
            {
                // 如果是Android的SteamingAssets路径，则使用插件BetterStreamingAssets读取
                if (Application.IsAndroid && Path.IsStreamingAssetsPath(path))
                {
                    return FileWithBSA.IsExists(path);
                }

                return System.IO.File.Exists(path);
            }

            /// <summary>
            /// 移动文件到目标目录
            /// </summary>
            /// <param name="sourceFileName">文件源路径</param>
            /// <param name="destFileName">目标路径</param>
            public static void Move(string sourceFileName, string destFileName)
            {
                if (!System.IO.File.Exists(sourceFileName)) return;
                Copy(sourceFileName, destFileName, true);
                Delete(sourceFileName);
            }

            /// <summary>
            /// 读取指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <returns></returns>
            public static byte[] ReadAllBytes(string path)
            {
                // 如果是Android的SteamingAssets路径，则使用插件BetterStreamingAssets读取
                if (Application.IsAndroid && Path.IsStreamingAssetsPath(path))
                {
                    return FileWithBSA.ReadAllBytes(path);
                }

                return System.IO.File.ReadAllBytes(path);
            }

            /// <summary>
            /// 读取指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <returns></returns>
            public static string ReadAllText(string path)
            {
                // 如果是Android的SteamingAssets路径，则使用插件BetterStreamingAssets读取
                if (Application.IsAndroid && Path.IsStreamingAssetsPath(path))
                {
                    return FileWithBSA.ReadAllText(path);
                }

                return System.IO.File.ReadAllText(path, Encoding.UTF8);
            }

            /// <summary>
            /// 读取指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="encoding">编码</param>
            /// <returns></returns>
            public static string[] ReadAllLines(string path, Encoding encoding)
            {
                // 如果是Android的SteamingAssets路径，则使用插件BetterStreamingAssets读取
                if (Application.IsAndroid && Path.IsStreamingAssetsPath(path))
                {
                    return FileWithBSA.ReadAllLines(path);
                }

                return System.IO.File.ReadAllLines(path, encoding);
            }

            /// <summary>
            /// 读取指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <returns></returns>
            public static string[] ReadAllLines(string path)
            {
                // 如果是Android的SteamingAssets路径，则使用插件BetterStreamingAssets读取
                if (Application.IsAndroid && Path.IsStreamingAssetsPath(path))
                {
                    return FileWithBSA.ReadAllLines(path);
                }

                return System.IO.File.ReadAllLines(path, Encoding.UTF8);
            }

            /// <summary>
            /// 写入指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="buffer">写入内容</param>
            /// <returns></returns>
            public static void WriteAllLines(string path, byte[] buffer) => System.IO.File.WriteAllBytes(path, buffer);

            /// <summary>
            /// 写入指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="lines">写入的内容</param>
            /// <param name="encoding">编码</param>
            /// <returns></returns>
            public static void WriteAllLines(string path, string[] lines, Encoding encoding) => System.IO.File.WriteAllLines(path, lines, encoding);

            /// <summary>
            /// 写入指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="lines">写入的内容</param>
            /// <returns></returns>
            public static void WriteAllLines(string path, string[] lines) => System.IO.File.WriteAllLines(path, lines, Encoding.UTF8);

            /// <summary>
            /// 写入指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="content">写入的内容</param>
            /// <param name="encoding">编码</param>
            /// <returns></returns>
            public static void WriteAllText(string path, string content, Encoding encoding) => System.IO.File.WriteAllText(path, content, encoding);

            /// <summary>
            /// 写入指定路径的文件内容，UTF-8
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="content">写入的内容</param>
            /// <returns></returns>
            public static void WriteAllText(string path, string content) => System.IO.File.WriteAllText(path, content, Encoding.UTF8);

            /// <summary>
            /// 写入指定路径的文件内容
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <param name="buffer">写入的内容</param>
            /// <returns></returns>
            public static void WriteAllBytes(string path, byte[] buffer) => System.IO.File.WriteAllBytes(path, buffer);
        }
    }
}