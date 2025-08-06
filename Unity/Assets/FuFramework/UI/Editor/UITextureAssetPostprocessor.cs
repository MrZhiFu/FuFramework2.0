using FuFramework.Core.Runtime;
using GameFrameX.Runtime;
using UnityEditor;

namespace FuFramework.UI.Editor
{
    /// <summary>
    /// UI纹理资源导入后处理器
    /// </summary>
    internal sealed class UITextureAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var isBundleUI = assetPath.Contains(PathHelper.Combine(Utility.Asset.Path.BundlesPath, "UI"));
            var isResourceUI = assetPath.Contains(PathHelper.Combine("Resources", "UI"));
            if (!isBundleUI && !isResourceUI) return;
            
            var textureImporter = (TextureImporter)assetImporter;
           
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