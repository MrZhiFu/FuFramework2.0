using UnityEditor;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
using AOT.Framework.Core.Utility;
namespace FuFramework.UI.Editor
{
    /// <summary>
    /// UI纹理资源导入后处理器。
    /// 目标：在导入UI纹理资源时，自动设置其导入参数，确保在不同平台上的正确显示。
    /// 功能：
    ///     1. 自动设置纹理导入参数，如纹理类型、Mipmap、可读性等。
    ///     2. 支持在不同平台上的正确显示，如Android、iOS、PC/Standalone等。
    /// </summary>
    internal sealed class UITextureAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var isBundleUI   = assetPath.Contains(UtilityAOT.Path.Combine(UtilityAOT.AssetPath.BundlesPath, "UI"));
            var isResourceUI = assetPath.Contains(UtilityAOT.Path.Combine("Resources",                   "UI"));
            if (!isBundleUI && !isResourceUI) return;

            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null) return;

            if (textureImporter.textureType != TextureImporterType.Default)
                textureImporter.textureType = TextureImporterType.Default;

            if (textureImporter.mipmapEnabled)
                textureImporter.mipmapEnabled = false;

            if (textureImporter.isReadable)
                textureImporter.isReadable = false;

            textureImporter.alphaSource         = TextureImporterAlphaSource.FromInput;
            textureImporter.alphaIsTransparency = true;

            // Android - ASTC 6x6
            var androidSettings = textureImporter.GetPlatformTextureSettings("Android");
            androidSettings.overridden         = true;
            androidSettings.format             = TextureImporterFormat.ASTC_6x6;
            androidSettings.compressionQuality = 100;
            textureImporter.SetPlatformTextureSettings(androidSettings);

            // iOS - ASTC 6x6
            var iosSettings = textureImporter.GetPlatformTextureSettings("iPhone");
            iosSettings.overridden         = true;
            iosSettings.format             = TextureImporterFormat.ASTC_6x6;
            iosSettings.compressionQuality = 100;
            textureImporter.SetPlatformTextureSettings(iosSettings);

            // PC/Standalone - DXT5 支持透明
            var standaloneSettings = textureImporter.GetPlatformTextureSettings("Standalone");
            standaloneSettings.overridden         = true;
            standaloneSettings.format             = TextureImporterFormat.DXT5;
            standaloneSettings.compressionQuality = 100;
            textureImporter.SetPlatformTextureSettings(standaloneSettings);

            // WebGL - DXT5 兼容性最好
            var webglSettings = textureImporter.GetPlatformTextureSettings("WebGL");
            webglSettings.overridden         = true;
            webglSettings.format             = TextureImporterFormat.DXT5;
            webglSettings.compressionQuality = 100;
            textureImporter.SetPlatformTextureSettings(webglSettings);
        }
    }
}