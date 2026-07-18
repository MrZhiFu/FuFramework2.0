using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class UtilityAOT
    {
        /// <summary>
        /// 应用相关的实用函数。
        /// 功能：
        ///     1. 提供平台相关的获取和判断。
        ///     2. 打开URL。
        /// </summary>
        public static class Application
        {
            /// <summary>
            /// 获取平台名称
            /// </summary>
            public static string PlatformName
            {
                get
                {
#if UNITY_ANDROID
                    return "Android";
#elif UNITY_STANDALONE_OSX
                    return "MacOs";
#elif UNITY_IOS || UNITY_IPHONE
                    return "iOS";
#elif UNITY_WEBGL
                    return "WebGL";
#elif UNITY_STANDALONE_WIN
                    return "Windows";
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 是否是编辑器
            /// </summary>
            public static bool IsEditor
            {
                get
                {
#if UNITY_EDITOR
                    return true;
#else
                    return false;
#endif
                }
            }

            /// <summary>
            /// 是否是安卓
            /// </summary>
            public static bool IsAndroid
            {
                get
                {
#if UNITY_ANDROID
                    return true;
#else
                    return false;
#endif
                }
            }

            /// <summary>
            /// 是否是WebGL平台
            /// </summary>
            public static bool IsWebGL
            {
                get
                {
#if UNITY_WEBGL
                    return true;
#else
                    return UnityEngine.Application.platform == RuntimePlatform.WebGLPlayer;
#endif
                }
            }

            /// <summary>
            /// 是否是Windows平台
            /// </summary>
            public static bool IsWindows
            {
                get
                {
#if UNITY_STANDALONE_WIN
                    return true;
#endif
                    return UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer;
                }
            }

            /// <summary>
            /// 是否是Linux平台
            /// </summary>
            public static bool IsLinux => UnityEngine.Application.platform == RuntimePlatform.LinuxPlayer;

            /// <summary>
            /// 是否是Mac平台
            /// </summary>
            public static bool IsMacOsx
            {
                get
                {
#if UNITY_STANDALONE_OSX
                    return true;
#endif
                    return UnityEngine.Application.platform == RuntimePlatform.OSXPlayer;
                }
            }

            /// <summary>
            /// 是否是iOS 移动平台
            /// </summary>
            public static bool IsIOS
            {
                get
                {
#if UNITY_IOS
                    return true;
#else
                    return false;
#endif
                }
            }

#if UNITY_IOS
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void open_url(string url);
#endif
            /// <summary>
            /// 打开URL
            /// </summary>
            /// <param name="url">url地址</param>
            public static void OpenURL(string url)
            {
                if (string.IsNullOrEmpty(url)) return;
#if UNITY_IOS
                open_url(url);
#else
                UnityEngine.Application.OpenURL(url);
#endif
            }
        }
    }
}