#if UNITY_EDITOR
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor.RedDot
{
    public partial class RedDotSettingEditor
    {
        /// <summary>
        /// 背景颜色作用域辅助类
        /// </summary>
        private class BackgroundColorScope : System.IDisposable
        {
            private readonly Color m_OriginalColor;

            public BackgroundColorScope(Color newColor)
            {
                m_OriginalColor     = GUI.backgroundColor;
                GUI.backgroundColor = newColor;
            }

            public void Dispose()
            {
                GUI.backgroundColor = m_OriginalColor;
            }
        }

        /// <summary>
        /// GUI颜色作用域辅助类
        /// </summary>
        private class GUIColorScope : System.IDisposable
        {
            private readonly Color m_OriginalColor;

            public GUIColorScope(Color newColor)
            {
                m_OriginalColor = GUI.color;
                GUI.color       = newColor;
            }

            public void Dispose()
            {
                GUI.color = m_OriginalColor;
            }
        }
    }
}
#endif