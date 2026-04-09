using Cysharp.Threading.Tasks;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 使用BetterStreamingAssets插件读取Android平台SteamingAssets中的文件相关实用函数。
        /// 功能：
        ///     1. 判断文件是否存在。
        ///     2. 读取指定路径的文件内容。
        /// 
        /// BetterStreamingAssets是一款旨在简化并优化Unity项目中流式资产操作的插件。
        /// 它通过模仿System.IO.File和System.IO.Directory的API设计，使得开发者能够以更低的开销直接访问游戏中的流式资产，特别是在对效率要求苛刻的Android平台。
        /// 注意：所有文件名应保持小写，并避免非ASCII字符的使用
        /// </summary>
        public static class FileWithBSA
        {
            /// <summary>
            /// 插件是否已初始化
            /// </summary>
            private static bool m_IsInited = false;

            /// <summary>
            /// 判断文件是否存在
            /// </summary>
            /// <param name="path">文件路径</param>
            /// <returns></returns>
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
            /// <returns></returns>
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
            /// <returns></returns>
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
            /// <returns></returns>
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
            /// <param name="path"></param>
            /// <returns></returns>
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
                if (m_IsInited) return;
                BetterStreamingAssets.Initialize();
                m_IsInited = true;
            }
        }
    }
}