// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 游戏版本号相关的实用函数。
        /// 功能：
        ///     1. 获取主版本号、次版本号、修订号。
        /// </summary>
        public static class Version
        {
            /// <summary>
            /// 获取完整版本号(如：1.0.0)。
            /// </summary>
            public static string FullVersion => UnityEngine.Application.version;

            /// <summary>
            /// 获取主版本号(如：1.0.0 => 1)。
            /// </summary>
            public static string MajorVersion => FullVersion.Split('.')[0];

            /// <summary>
            /// 获取次版本号(如：1.0.0 => 0)。
            /// </summary>
            public static string MinorVersion => FullVersion.Split('.')[1];

            /// <summary>
            /// 获取修订号(如：1.0.0 => 0)。
            /// </summary>
            public static string ReviseVersion => FullVersion.Split('.')[2];

            /// <summary>
            /// 获取主版本号+次版本号(如：1.0.0 => 1.0)。
            /// </summary>
            public static string MajorMinorVersion => $"{MajorVersion}.{MinorVersion}";
        }
    }
}