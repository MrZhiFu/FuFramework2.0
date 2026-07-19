using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 相机相关的扩展方法。
    /// 功能：
    ///     1. 获取相机快照。
    /// </summary>
    public static class CameraEx
    {
        /// <summary>
        /// 获取相机快照
        /// </summary>
        /// <param name="camera">相机</param>
        /// <param name="scale">缩放比</param>
        /// <returns>相机快照纹理对象</returns>
        public static Texture2D GetCaptureScreenshot(this Camera camera, float scale = 0.5f)
        {
            var rect          = new Rect(0, 0, Screen.width * scale, Screen.height * scale);
            var name          = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var renderTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0);
            renderTexture.name   = SceneManager.GetActiveScene().name + "_" + renderTexture.width + "_" + renderTexture.height + "_" + name;
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            var screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false)
            {
                name = renderTexture.name
            };
            screenShot.ReadPixels(rect, 0, 0);
            screenShot.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            return screenShot;
        }

        /// <summary>
        /// 判断渲染组件是否在相机范围内
        /// </summary>
        /// <param name="camera">相机</param>
        /// <param name="renderer">渲染组件</param>
        /// <returns>如果渲染组件在相机范围内返回true，否则返回false</returns>
        public static bool IsVisibleFrom(this Camera camera, Renderer renderer)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }
    }
}
