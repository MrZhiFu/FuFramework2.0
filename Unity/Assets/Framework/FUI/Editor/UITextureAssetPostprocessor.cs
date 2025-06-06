using GameFrameX.Runtime;
using UnityEditor;

namespace GameFrameX.UI.Editor
{
    /// <summary>
    /// UI纹理资源导入后处理器
    /// </summary>
    internal sealed class UITextureAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains(PathHelper.Combine(Utility.Asset.Path.BundlesPath, "UI"))) return;
            
            var textureImporter = (TextureImporter)assetImporter;
           
            if (textureImporter.textureType != TextureImporterType.Default)
                textureImporter.textureType = TextureImporterType.Default;

            if (textureImporter.mipmapEnabled)
                textureImporter.mipmapEnabled = false;

            textureImporter.alphaSource         = TextureImporterAlphaSource.FromInput;
            textureImporter.alphaIsTransparency = true;
        }
    }
}