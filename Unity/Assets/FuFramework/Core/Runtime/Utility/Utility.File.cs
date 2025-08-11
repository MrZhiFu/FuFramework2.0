// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 文件相关的实用函数
        /// </summary>
        public static class File
        {
            /// <summary>
            /// 字节大小单位列表
            /// </summary>
            private static readonly string[] s_UnitList = {"B", "KB", "MB", "GB", "TB", "PB"};

            /// <summary>
            /// 获取字节大小
            /// </summary>
            /// <param name="size">字节大小</param>
            /// <returns>格式化后的字节大小字符串</returns>
            public static string GetBytesSize(long size)
            {
                foreach (var unit in s_UnitList)
                {
                    if (size <= 1024)
                    {
                        return size + unit;
                    }

                    size /= 1024;
                }

                return size + s_UnitList[0];
            }
        }
    }
}