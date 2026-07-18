using System;
using FairyGUI;
using UnityEngine;
using FuFramework.UI.Runtime;
using Hotfix.Config;
using Hotfix.RedDot;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
{
    public partial class CompRedDot
    {
        /// <summary>
        /// 红点显示模式（与 ERedDotDisplayMode 枚举值对齐）
        /// </summary>
        public enum DisplayMode
        {
            /// <summary>只显示红点</summary>
            DotOnly = 0,
            /// <summary>红点 + 数字</summary>
            DotNumber = 1,
            /// <summary>=1 显示红点，>1 显示数字</summary>
            Auto = 2
        }

        /// <summary>
        /// 静态节点 Key（枚举，DisplayMode 由配置表决定）
        /// </summary>
        private ERedDotKey? m_StaticKey;

        /// <summary>
        /// 动态节点 Key（字符串，默认 DotOnly）
        /// </summary>
        private string m_DynamicKey;

        /// <summary>
        /// 缓存目标组件（用于 SetRedDotPos）
        /// </summary>
        private GComponent m_Target;

        /// <summary>
        /// 初始化（在 InitRedDot 之后调用）
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
        /// 销毁
        /// </summary>
        private void OnDispose()
        {
            if (m_StaticKey.HasValue)
                RedDotModule.Instance.Unregister(m_StaticKey.Value, OnRedDotChanged);
            else if (m_DynamicKey != null)
                RedDotModule.Instance.Unregister(m_DynamicKey, OnRedDotChanged);
        }

        /// <summary>
        /// 静态节点注册（枚举，DisplayMode 由配置表决定）
        /// </summary>
        /// <param name="view">所属界面</param>
        /// <param name="redKey">红点节点 Key</param>
        public void Register(ViewBase view, ERedDotKey redKey)
        {
            if (view == null) return;

            uiView = view;
            m_StaticKey = redKey;
            RedDotModule.Instance.Register(redKey, OnRedDotChanged);
        }

        /// <summary>
        /// 动态节点注册（字符串，默认 DotOnly）
        /// </summary>
        /// <param name="view">所属界面</param>
        /// <param name="redKey">动态红点节点 Key</param>
        public void Register(ViewBase view, string redKey)
        {
            if (view == null) return;
            if (string.IsNullOrEmpty(redKey)) return;

            uiView = view;
            m_DynamicKey = redKey;
            RedDotModule.Instance.Register(redKey, OnRedDotChanged);
        }

        /// <summary>
        /// 从 RedDotNode 配置读取 DisplayMode
        /// </summary>
        private DisplayMode GetDisplayMode()
        {
            if (m_StaticKey.HasValue)
            {
                var node = RedDotModule.Instance.GetNode(m_StaticKey.Value);
                return (DisplayMode)(int)(node?.DisplayMode ?? ERedDotDisplayMode.DotOnly);
            }
            return DisplayMode.DotOnly; // 动态节点默认 DotOnly
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
            var mode = GetDisplayMode();
            switch (mode)
            {
                case DisplayMode.DotOnly:
                    txtCount.visible = false;
                    imgRedDot.visible = redCount > 0;
                    break;
                case DisplayMode.DotNumber:
                    txtCount.visible = redCount >= 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text = FormatRedDotCount(redCount);
                    break;
                case DisplayMode.Auto:
                    txtCount.visible = redCount > 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text = FormatRedDotCount(redCount);
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
                _ => count.ToString()
            };
        }
    }
}
