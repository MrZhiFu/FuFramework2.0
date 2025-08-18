#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using System.IO;
using UnityEngine;

namespace FuFramework.GetChannel.Runtime
{
    /// <summary>
    /// 获取不同操作系统平台下的渠道名称
    /// Android:
    ///         需要在主启动的Activity 中添加
    /// <meta-data android:name="channel" android:value="android_cn_taptap" />
    ///
    /// iOS :
    ///         需要在Info.plist中添加
    ///         Key     ==> value    String 类型
    ///         channel ==> ios_cn_xxx    
    /// 
    /// </summary>
    public sealed class BlankGetChannel
    {
#if UNITY_IOS
	[DllImport("__Internal")]
	private static extern string getChannelName(string channelKey);

#endif
        /// <summary>
        /// 获取渠道值
        /// </summary>
        public static string GetChannelName(string channelKey = "channel")
        {
            var channelName = "default";
#if UNITY_STANDALONE || UNITY_EDITOR
            var path = Application.streamingAssetsPath + "/channel.txt";
            if (!File.Exists(path)) return channelName;
            var channel = File.ReadAllText(Application.streamingAssetsPath + "/channel.txt");
            if (!string.IsNullOrEmpty(channel)) channelName = channel;

#elif UNITY_ANDROID
        using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.alianhome.getchannel.MainActivity"))
        {
            channelName = androidJavaClass.CallStatic<string>("GetChannel", channelKey);
        }
#elif UNITY_IOS
        channelName = getChannelName(channelKey);
#endif
            return channelName;
        }
    }
}