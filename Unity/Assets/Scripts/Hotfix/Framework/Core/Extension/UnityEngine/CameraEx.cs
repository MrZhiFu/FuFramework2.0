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
            // RT 尺寸与 ReadPixels 的 rect 必须用同一组整数尺寸：
            // 原实现 RT 用 (int) 截断、rect 用浮点，非整数倍缩放下二者不一致（ReadPixels 读取区域错位/越界）
            var width  = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale));
            var rect   = new Rect(0, 0, width, height);
            var name   = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            var renderTexture = RenderTexture.GetTemporary(width, height, 0);
            renderTexture.name = SceneManager.GetActiveScene().name + "_" + width + "_" + height + "_" + name;

            // 保存并恢复相机/渲染目标：异常路径也必须还原，否则相机 targetTexture 与 RenderTexture.active 被污染
            var previousActiveTexture = RenderTexture.active;
            var previousTargetTexture = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var screenShot = new Texture2D(width, height, TextureFormat.RGB24, false)
                {
                    name = renderTexture.name
                };
                screenShot.ReadPixels(rect, 0, 0);
                screenShot.Apply();
                return screenShot;
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
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
