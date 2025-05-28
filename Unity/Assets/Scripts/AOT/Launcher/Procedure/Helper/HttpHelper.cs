using System.Collections.Generic;
using UnityEngine;
using GameFrameX.Runtime;


/// <summary>
/// Http网络请求后台辅助类。
/// 用于给各个平台提供Http网络请求的基础参数，如语言、版本号、设备ID等。
/// </summary>
public static class HttpHelper
{
    private static readonly Dictionary<string, object> s_ParamDict = new();

    /// <summary>
    /// 获取各个平台的请求游戏后台的基础参数。如语言、版本号、包名，渠道等。
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, object> GetBaseParams()
    {
            //@formatter:off
        s_ParamDict["Language"]               = Application.systemLanguage.ToString();  // 语言
        s_ParamDict["AppVersion"]             = Application.version;                    // 版本号
        s_ParamDict["DeviceUniqueIdentifier"] = SystemInfo.deviceUniqueIdentifier;      // 设备ID用于判断白名单和其他识别用途

#if UNITY_WEBGL
        // WebGL平台参数
        DictionaryParams["PackageName"] = "com.smartdog.bbqgame"; // 游戏包名
        
        #if ENABLE_WECHAT_MINI_GAME
                // 微信小游戏
                DictionaryParams["Channel"]    = "WxMiniGame";     // 微信小游戏渠道
                DictionaryParams["SubChannel"] = "WxMiniGame";     // 微信小游戏子渠道
                DictionaryParams["Platform"]   = "WebGLWxMiniGame";// 微信小游戏平台

        #elif ENABLE_DOUYIN_MINI_GAME
                // 抖音小游戏
                DictionaryParams["Channel"]    = "DouYinMiniGame";     // 抖音小游戏渠道
                DictionaryParams["SubChannel"] = "DouYinMiniGame";     // 抖音小游戏子渠道
                DictionaryParams["Platform"]   = "WebGLDouYinMiniGame";// 抖音小游戏平台

        #else
                DictionaryParams["Channel"]    = "WebGL";// 其他WebGL平台渠道
                DictionaryParams["SubChannel"] = "WebGL";// 其他WebGL平台子渠道
                DictionaryParams["Platform"]   = PathHelper.GetPlatformName;// 其他WebGL平台
#endif

#else
        // 其他平台参数
        s_ParamDict["Platform"] = PathHelper.GetPlatformName;
        
        #if UNITY_STANDALONE_WIN
                s_ParamDict["PackageName"] = Application.productName;
        #else
                DictionaryParams["PackageName"] = Application.identifier;
        #endif
            
        s_ParamDict["Channel"]    = BlankGetChannel.GetChannelName();
        s_ParamDict["SubChannel"] = BlankGetChannel.GetChannelName();
#endif
        return s_ParamDict;
        
        //@formatter:on
    }
}