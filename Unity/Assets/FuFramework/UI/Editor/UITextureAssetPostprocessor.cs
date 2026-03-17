using UnityEditor;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Editor
{
    /// <summary>
    /// UI纹理资源导入后处理器
    /// </summary>
    internal sealed class UITextureAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var isBundleUI = assetPath.Contains(Utility.Path.Combine(Utility.AssetPath.BundlesPath, "UI"));
            var isResourceUI = assetPath.Contains(Utility.Path.Combine("Resources", "UI"));
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
        }
    }
}