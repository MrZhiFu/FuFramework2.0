using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 相机帮助类
    /// </summary>
    public static class CameraEx
    {
        /// <summary>
        /// 获取相机快照
        /// </summary>
        /// <param name="main">相机</param>
        /// <param name="scale">缩放比</param>
        public static Texture2D GetCaptureScreenshot(this Camera main, float scale = 0.5f)
        {
            var rect          = new Rect(0, 0, Screen.width * scale, Screen.height * scale);
            var name          = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var renderTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0);
            renderTexture.name = SceneManager.GetActiveScene().name + "_" + renderTexture.width + "_" + renderTexture.height + "_" + name;
            main.targetTexture = renderTexture;
            main.Render();

            RenderTexture.active = renderTexture;
            var screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false)
            {
                name = renderTexture.name
            };
            screenShot.ReadPixels(rect, 0, 0);
            screenShot.Apply();
            main.targetTexture   = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            return screenShot;
        }
    }
}