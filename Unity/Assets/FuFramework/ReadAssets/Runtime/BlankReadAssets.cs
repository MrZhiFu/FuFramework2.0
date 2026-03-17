using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ReadAssets.Runtime
{
    /// <summary>
    /// Android平台读取Assets类
    /// </summary>
    public static class BlankReadAssets
    {
        private static AndroidJavaClass m_AndroidJavaClass;

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="path">相对目录</param>
        /// <returns></returns>
        public static byte[] Read(string path)
        {
            Guard();
            return m_AndroidJavaClass.CallStatic<byte[]>("readFile", path);
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="path">相对目录</param>
        /// <returns></returns>
        public static bool IsFileExists(string path)
        {
            Guard();
            return m_AndroidJavaClass.CallStatic<bool>("isFileExists", path);
        }

        private static void Guard()
        {
            m_AndroidJavaClass ??= new AndroidJavaClass("com.alianblank.readassets.MainActivity");
        }
    }
}