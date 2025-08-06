using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 默认游戏框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : GameFrameworkLog.ILogHelper
    {
        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(GameFrameworkLogLevel level, object message)
        {
            var time = $"[Unity]:[{DateTime.Now:HH:mm:ss.fff}]:";

            switch (level)
            {
                // @formatter:off
                case GameFrameworkLogLevel.Debug:   Debug.Log($"{time}{message}");        break;
                case GameFrameworkLogLevel.Info:    Debug.Log($"{time}{message}");        break;
                case GameFrameworkLogLevel.Warning: Debug.LogWarning($"{time}{message}"); break;
                case GameFrameworkLogLevel.Error:   Debug.LogError($"{time}{message}");   break;
                case GameFrameworkLogLevel.Fatal:   Debug.LogError($"{time}{message}");   break;
                default:                            throw new GameFrameworkException($"{time}{message}");
                // @formatter:on
            }
        }
    }
}