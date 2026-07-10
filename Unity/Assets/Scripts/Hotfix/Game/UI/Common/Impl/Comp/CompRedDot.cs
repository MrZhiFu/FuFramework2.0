using System;
using FairyGUI;
using UnityEngine;
using FuFramework.UI.Runtime;
using Hotfix.RedDot;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
{
    public partial class CompRedDot
    {
        /// <summary>
        /// 红点显示模式
        /// </summary>
        public enum EDisplayMode
        {
            /// <summary>
            /// 只显示红点
            /// </summary>
            DotOnly,

            /// <summary>
            /// 红点+数字
            /// </summary>
            DotNumber,

            /// <summary>
            /// 根据数量自动显示，=1显示红点，>1显示数字
            /// </summary>
            Auto
        }

        /// <summary>
        /// 红点Key
        /// </summary>
        private string m_Key;

        /// <summary>
        /// 红点显示模式
        /// </summary>
        private EDisplayMode m_DisplayMode = EDisplayMode.DotOnly;

        /// <summary>
        /// 缓存目标组件
        /// </summary>
        private GComponent m_Target;

        /// <summary>
        /// 初始化
        /// </summary>
        private void OnInit()
        {
            InitEvent();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent() { }

        /// <summary>
        /// 销毁。
        /// </summary>
        private void OnDispose()
        {
            RedDotModule.Instance.Unregister(m_Key, OnRedDotChanged);
        }

        /// <summary>
        /// 注册红点
        /// </summary>
        /// <param name="view">所属界面</param>
        /// <param name="target">红点依附的目标组件</param>
        /// <param name="redKey">红点Key</param>
        /// <param name="displayMode">红点显示模式</param>
        public void Register(ViewBase view, GComponent target, string redKey, EDisplayMode displayMode = EDisplayMode.DotOnly)
        {
            if (view   == null) return;
            if (target == null) return;
            if (string.IsNullOrEmpty(redKey)) return;

            uiView        = view;
            m_Target      = target;
            m_Key         = redKey;
            m_DisplayMode = displayMode;

            // 注册红点变化事件
            RedDotModule.Instance.Register(m_Key, OnRedDotChanged);
        }

        /// <summary>
        /// 手动设置红点。
        /// 如在滑动列表的Item上显示红点，红点数量变化时，需要手动调用此方法刷新红点显示。
        /// </summary>
        public void SetRedDot(int redCount) => OnRedDotChanged(redCount);

        /// <summary>
        /// 设置红点位置，默认在组件的右上角
        /// </summary>
        /// <param name="offset">位置偏移</param>
        public void SetRedDotPos(Vector2 offset = default)
        {
            if (m_Target == null) return;

            // 计算在父容器内的相对位置
            var posX = m_Target.width - width + offset.x;
            var posY = offset.y;

            SetXY(posX, posY);
        }

        /// <summary>
        /// 红点变化事件回调
        /// </summary>
        /// <param name="redCount">红点数量</param>
        private void OnRedDotChanged(int redCount)
        {
            switch (m_DisplayMode)
            {
                case EDisplayMode.DotOnly:
                    txtCount.visible  = false;
                    imgRedDot.visible = redCount > 0;
                    break;
                case EDisplayMode.DotNumber:
                    txtCount.visible  = redCount >= 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text     = FormatRedDotCount(redCount);
                    break;
                case EDisplayMode.Auto:
                    txtCount.visible  = redCount > 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text     = FormatRedDotCount(redCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 格式化红点数量显示
        /// </summary>
        /// <param name="count">数量</param>
        /// <returns>格式化后的字符串</returns>
        private static string FormatRedDotCount(int count)
        {
            return count switch
            {
                <= 0 => "0",
                > 99 => "99+",
                _    => count.ToString()
            };
        }
    }
}