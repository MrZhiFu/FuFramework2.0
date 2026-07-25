using System;
using FairyGUI;
using UnityEngine;
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
        /// 红点节点 Key（从 customData 自动解析）
        /// </summary>
        private ERedDotKey? m_RedDotKey;

        /// <summary>
        /// 缓存目标组件（用于 SetRedDotPos）
        /// </summary>
        private GComponent m_Target;

        /// <summary>
        /// 初始化：自动解析 customData 中的 red_dot:&lt;key&gt; 并订阅 EventModule
        /// </summary>
        private void OnInit()
        {
            var customData = data as string;
            if (!RedDotDataParser.TryParse(customData, out var key)) return;
           
            m_RedDotKey = key;
            GlobalModule.EventModule.Subscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);

            // 立即刷新当前状态
            var state = RedDotModule.Instance.GetState(key);
            RefreshUI(state.Count, state.DisplayMode);
        }

        /// <summary>
        /// 销毁：取消 EventModule 订阅
        /// </summary>
        private void OnDispose()
        {
            if (m_RedDotKey.HasValue)
                GlobalModule.EventModule.Unsubscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);
        }

        /// <summary>
        /// 设置红点位置，默认在组件的右上角
        /// </summary>
        /// <param name="offset">位置偏移</param>
        public void SetRedDotPos(Vector2 offset = default)
        {
            if (m_Target == null) return;

            var posX = m_Target.width - width + offset.x;
            var posY = offset.y;
            SetXY(posX, posY);
        }

        /// <summary>
        /// 手动设置红点显示（用于列表 Item 等脱离 RedDotModule 的场景）
        /// </summary>
        /// <param name="redCount">红点数量</param>
        /// <param name="mode">显示模式（默认 DotOnly）</param>
        public void SetRedDot(int redCount, ERedDotDisplayMode mode = ERedDotDisplayMode.DotOnly)
        {
            RefreshUI(redCount, mode);
        }

        #region 内部实现

        /// <summary>
        /// EventModule 回调：本帧红点变更时检查是否需要刷新
        /// </summary>
        private void OnRedDotChanged(object sender, GameEventArgs e)
        {
            if (!m_RedDotKey.HasValue) return;
            if (e is not RedDotChangedEventArgs args) return;

            for (int i = 0; i < args.ChangedStaticKeys.Count; i++)
            {
                if (args.ChangedStaticKeys[i] == m_RedDotKey.Value)
                {
                    var state = RedDotModule.Instance.GetState(m_RedDotKey.Value);
                    RefreshUI(state.Count, state.DisplayMode);
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

        #endregion
    }
}
