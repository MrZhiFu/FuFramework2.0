using System;
using Hotfix.Game.Config;

namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 解析 FairyGUI customData 中的 red_dot:{redDotId} 段
    /// 支持与其他插件组合（竖线分隔，如 i18n&key|red_dot:TaskTab）
    /// </summary>
    public static class RedDotDataParser
    {
        private const string FlagRedDot = "red_dot:";

        /// <summary>
        /// 尝试从 customData 解析红点 Key
        /// </summary>
        /// <returns>解析成功返回 true，失败返回 false</returns>
        public static bool TryParse(string customData, out ERedDotKey result)
        {
            result = 0;

            if (string.IsNullOrEmpty(customData))
                return false;

            int segStart = customData.IndexOf(FlagRedDot, StringComparison.Ordinal);
            if (segStart < 0)
                return false;

            int dataStart = segStart + FlagRedDot.Length;
            int pipePos = customData.IndexOf('|', dataStart);
            string segValue = pipePos >= 0
                ? customData.Substring(dataStart, pipePos - dataStart).Trim()
                : customData.Substring(dataStart).Trim();

            if (string.IsNullOrEmpty(segValue))
                return false;

            // 尝试解析为整数
            if (int.TryParse(segValue, out int idInt))
            {
                result = (ERedDotKey)idInt;
                return true;
            }

            // 尝试解析为枚举名（忽略大小写）
            if (Enum.TryParse(segValue, true, out result))
                return true;

            return false;
        }
    }
}
