using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 文件帮助类
    /// </summary>
    public static class FileHelper
    {
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
                File.Delete(subFile);
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
                File.Copy(file.FullName, Path.Combine(target.FullName, file.Name), true);
            }

            var dirs = source.GetDirectories();
            foreach (var dir in dirs)
            {
                CopyDirectory(dir.FullName, Path.Combine(target.FullName, dir.Name));
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
            if (!File.Exists(sourceFileName)) return;
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path">文件路径</param>
        public static void Delete(string path) => File.Delete(path);

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        public static bool IsExists(string path)
        {
#if ENABLE_GAME_FRAME_X_READ_ASSETS
            if (IsAndroidReadOnlyPath(path, out var readPath))
            {
                return BlankReadAssets.BlankReadAssets.IsFileExists(readPath);
            }
#endif
            return File.Exists(path);
        }


        /// <summary>
        /// 判断是否是Android的只读路径
        /// </summary>
        /// <param name="path"></param>
        /// <param name="readPath"></param>
        /// <returns></returns>
        public static bool IsAndroidReadOnlyPath(string path, out string readPath)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (PathHelper.NormalizePath(path).Contains(PathHelper.AppResPath))
                {
                    readPath = path.Substring(PathHelper.AppResPath.Length);
                    return true;
                }
            }

            readPath = null;
            return false;
        }

        /// <summary>
        /// 移动文件到目标目录
        /// </summary>
        /// <param name="sourceFileName">文件源路径</param>
        /// <param name="destFileName">目标路径</param>
        public static void Move(string sourceFileName, string destFileName)
        {
            if (!File.Exists(sourceFileName)) return;
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
#if ENABLE_GAME_FRAME_X_READ_ASSETS
            if (IsAndroidReadOnlyPath((path), out var readPath))
            {
                return BlankReadAssets.BlankReadAssets.Read(readPath);
            }
#endif

            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// 读取指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string ReadAllText(string path, Encoding encoding) => File.ReadAllText(path, encoding);

        /// <summary>
        /// 读取指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        public static string ReadAllText(string path) => File.ReadAllText(path, Encoding.UTF8);

        /// <summary>
        /// 读取指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string[] ReadAllLines(string path, Encoding encoding) => File.ReadAllLines(path, encoding);

        /// <summary>
        /// 读取指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        public static string[] ReadAllLines(string path) => File.ReadAllLines(path, Encoding.UTF8);

        /// <summary>
        /// 写入指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="buffer">写入内容</param>
        /// <returns></returns>
        public static void ReadAllLines(string path, byte[] buffer) => File.WriteAllBytes(path, buffer);

        /// <summary>
        /// 写入指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="lines">写入的内容</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static void WriteAllLines(string path, string[] lines, Encoding encoding) => File.WriteAllLines(path, lines, encoding);

        /// <summary>
        /// 写入指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="lines">写入的内容</param>
        /// <returns></returns>
        public static void WriteAllLines(string path, string[] lines) => File.WriteAllLines(path, lines, Encoding.UTF8);

        /// <summary>
        /// 写入指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">写入的内容</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static void WriteAllText(string path, string content, Encoding encoding) => File.WriteAllText(path, content, encoding);

        /// <summary>
        /// 写入指定路径的文件内容，UTF-8
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">写入的内容</param>
        /// <returns></returns>
        public static void WriteAllText(string path, string content) => File.WriteAllText(path, content, Encoding.UTF8);

        /// <summary>
        /// 写入指定路径的文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="buffer">写入的内容</param>
        /// <returns></returns>
        public static void WriteAllBytes(string path, byte[] buffer) => File.WriteAllBytes(path, buffer);
    }
}