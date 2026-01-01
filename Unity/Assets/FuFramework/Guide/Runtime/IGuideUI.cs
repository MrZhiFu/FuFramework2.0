using System;
using UnityEngine;

namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// 引导UI接口
    /// </summary>
    public interface IGuideUI
    {
        /// <summary>
        /// 显示对话
        /// </summary>
        public void ShowDialog(string content, Action onConfirm = null);

        /// <summary>
        /// 隐藏对话
        /// </summary>
        public void HideDialog();

        /// <summary>
        /// 显示高亮
        /// </summary>
        public void ShowHighlight(string targetPath, Vector2 position);

        /// <summary>
        /// 隐藏高亮
        /// </summary>
        public void HideHighlight();

        /// <summary>
        /// 隐藏所有UI元素
        /// </summary>
        public void HideAll();
    }
}