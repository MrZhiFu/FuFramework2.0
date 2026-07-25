using System;
using FairyGUI;
using Hotfix.Framework.UI;
using Hotfix.Framework.RedDot;
using Hotfix.Framework.Event;
using Hotfix.Framework.Core;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class CompRedDot
    {
        /// <summary>
        /// FGUI customData 中红点标识前缀
        /// </summary>
        private const string FlagRedDot = "red_dot:";

        /// <summary>
        /// 静态红点节点 Key（customData 解析为 ERedDotKey 枚举时使用）
        /// </summary>
        private ERedDotKey? m_StaticKey;

        /// <summary>
        /// 动态红点节点 Key（customData 无法解析为枚举时作为 string 使用）
        /// </summary>
        private string m_DynamicKey;

        /// <summary>
        /// 初始化：自动解析 customData 中的 red_dot:&lt;key&gt; 并订阅红点变更事件
        /// </summary>
        private void OnInit()
        {
            var customData = data as string;
            if (!TryParseRedDotKey(customData, out var keyValue)) return;

            if (Enum.TryParse<ERedDotKey>(keyValue, true, out var staticKey))
            {
                m_StaticKey = staticKey;
            }
            else
            {
                m_DynamicKey = keyValue;
            }

            GlobalModule.EventModule.Subscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);
            RefreshCurrentState();
        }

        /// <summary>
        /// 销毁：取消订阅红点变更事件
        /// </summary>
        private void OnDispose()
        {
            if (m_StaticKey.HasValue || m_DynamicKey != null)
                GlobalModule.EventModule.Unsubscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);
        }

        #region 内部实现

        /// <summary>
        /// EventModule 回调：本帧红点变更时检查是否需要刷新
        /// </summary>
        private void OnRedDotChanged(object sender, GameEventArgs e)
        {
            if (e is not RedDotChangedEventArgs args) return;

            if (m_StaticKey.HasValue)
            {
                foreach (var key in args.ChangedStaticKeys)
                {
                    if (key != m_StaticKey.Value) continue;
                    RefreshCurrentState();
                    return;
                }
            }
            else if (m_DynamicKey != null)
            {
                foreach (var key in args.ChangedDynamicKeys)
                {
                    if (key != m_DynamicKey) continue;
                    RefreshCurrentState();
                    return;
                }
            }
        }

        /// <summary>
        /// 根据显示模式刷新 UI 控件
        /// </summary>
        private void RefreshUI(int redCount, ERedDotDisplayMode mode)
        {
            switch (mode)
            {
                case ERedDotDisplayMode.DotOnly:
                    txtCount.visible = false;
                    imgRedDot.visible = redCount > 0;
                    break;
                case ERedDotDisplayMode.DotNumber:
                    txtCount.visible = redCount >= 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text = FormatRedDotCount(redCount);
                    break;
                case ERedDotDisplayMode.Auto:
                    txtCount.visible = redCount > 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text = FormatRedDotCount(redCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        /// <summary>
        /// 格式化红点数量显示
        /// </summary>
        private static string FormatRedDotCount(int count)
        {
            return count switch
            {
                <= 0 => "0",
                > 99 => "99+",
                _ => count.ToString()
            };
        }

        /// <summary>
        /// 查询当前状态并刷新 UI
        /// </summary>
        private void RefreshCurrentState()
        {
            var state = m_StaticKey.HasValue
                ? RedDotModule.Instance.GetState(m_StaticKey.Value)
                : RedDotModule.Instance.GetState(m_DynamicKey);

            RefreshUI(state.Count, state.DisplayMode);
        }

        /// <summary>
        /// 解析UI组件customData中的 red_dot:{key} 段（支持竖线分隔组合，如red_dot:Bag）
        /// </summary>
        private static bool TryParseRedDotKey(string customData, out string result)
        {
            result = null;

            if (string.IsNullOrEmpty(customData))
                return false;

            var segStart = customData.IndexOf(FlagRedDot, StringComparison.Ordinal);
            if (segStart < 0)
                return false;

            var dataStart = segStart + FlagRedDot.Length;
            var pipePos = customData.IndexOf('|', dataStart);
            result = pipePos >= 0
                ? customData.Substring(dataStart, pipePos - dataStart).Trim()
                : customData.Substring(dataStart).Trim();

            return !string.IsNullOrEmpty(result);
        }

        #endregion
    }
}
