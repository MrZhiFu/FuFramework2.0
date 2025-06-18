using System.Text;
using UnityEngine;

namespace GameFrameX.Runtime
{
    public static class PathHelper
    {
        /// <summary>
        /// 热更新资源路径(应用程序外部资源路径存放路径)
        /// </summary>
        public static string AppHotfixResPath => $"{Application.persistentDataPath}/{Application.productName}/";

        /// <summary>
        /// 应用程序内部资源路径存放路径
        /// </summary>
        public static string AppResPath => NormalizePath(Application.streamingAssetsPath);

        /// <summary>
        /// 应用程序内部资源路径存放路径(www/webrequest专用)
        /// </summary>
        public static string AppResPath4Web
        {
            get
            {
#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR
                return $"file://{Application.streamingAssetsPath}";
#else
                return NormalizePath(Application.streamingAssetsPath);
#endif
            }
        }

        /// <summary>
        /// 获取平台名称
        /// </summary>
        public static string GetPlatformName
        {
            get
            {
#if UNITY_ANDROID
                return $"Android";
#elif UNITY_STANDALONE_OSX
                return $"MacOs";
#elif UNITY_IOS || UNITY_IPHONE
                return $"iOS";
#elif UNITY_WEBGL
                return $"WebGL";
#elif UNITY_STANDALONE_WIN
                return $"Windows";
#else
                return string.Empty;
#endif
            }
        }

        /// <summary>
        /// 规范化路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizePath(string path) => path.Replace('\\', '/').Replace("\\", "/");

        /// <summary>
        /// 合并路径的StringBuilder
        /// </summary>
        private static readonly StringBuilder CombinePathSb = new();

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
                if (path.EndsWithFast(separatorA) || path.EndsWithFast(separatorB)) continue;
                if (path.StartsWithFast(separatorA) || path.StartsWithFast(separatorB)) continue;
                CombinePathSb.Append(separatorA);
            }

            CombinePathSb.Append(paths[^1]); // ^1表示最后一个元素
            return CombinePathSb.ToString();
        }
    }
}