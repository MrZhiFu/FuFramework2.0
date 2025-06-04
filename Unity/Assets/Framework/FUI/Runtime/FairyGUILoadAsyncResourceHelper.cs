using System;
using System.Collections.Generic;
using System.IO;
using FairyGUI;
using GameFrameX.Asset.Runtime;
using GameFrameX.Runtime;
using UnityEngine;
using YooAsset;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class FairyGUILoadAsyncResourceHelper : IAsyncResource
    {
        private readonly Dictionary<string, UIPackageData> m_UIPackages = new Dictionary<string, UIPackageData>(32);

        /// <summary>
        /// 释放UI包
        /// </summary>
        /// <param name="uiPackageName"></param>
        public void ReleasePackage(string uiPackageName)
        {
            if (m_UIPackages.TryGetValue(uiPackageName, out var uiPackageData))
            {
                AssetComponent.UnloadAsset(uiPackageData.DefiledAssetPath);
                AssetComponent.UnloadAsset(uiPackageData.ResourceAssetPath);
                uiPackageData.Dispose();
                m_UIPackages.Remove(uiPackageName);
            }
        }

        /// <summary>
        /// 释放所有UI包
        /// </summary>
        public void ReleaseAllPackage()
        {
            foreach (var kv in m_UIPackages)
            {
                AssetComponent.UnloadAsset(kv.Value.DefiledAssetPath);
                AssetComponent.UnloadAsset(kv.Value.ResourceAssetPath);
                kv.Value.Dispose();
            }

            m_UIPackages.Clear();
        }

        private AssetComponent _assetComponent;

        private AssetComponent AssetComponent
        {
            get
            {
                if (_assetComponent == null)
                {
                    _assetComponent = GameEntry.GetComponent<AssetComponent>();
                }

                return _assetComponent;
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <param name="uiPackageName">UI包名称</param>
        /// <param name="extension">扩展名</param>
        /// <param name="type">资源类型</param>
        /// <param name="action"></param>
        public async void LoadResource(string assetName, string uiPackageName, string extension, PackageItemType type, Action<bool, string, object> action)
        {
            if (!m_UIPackages.TryGetValue(uiPackageName, out var uiPackageData))
            {
                uiPackageData = new UIPackageData(uiPackageName);
                m_UIPackages.Add(uiPackageName, uiPackageData);
            }


            if (type == PackageItemType.Misc)
            {
                // 描述文件
                AssetHandle assetHandle;
                if (uiPackageData.DefiledAssetHandle == null)
                {
                    assetHandle = await AssetComponent.LoadAssetAsync(assetName);
                    uiPackageData.SetDefiledAssetHandle(assetHandle, assetName);
                }
                else
                {
                    assetHandle = uiPackageData.DefiledAssetHandle;
                }

                action.Invoke(assetHandle != null && assetHandle.AssetObject != null, assetName, assetHandle?.GetAssetObject<TextAsset>());
                return;
            }

            if (type == PackageItemType.Image || type == PackageItemType.Atlas) //如果FGUI导出时没有选择分离通明通道，会因为加载不到!a结尾的Asset而报错，但是不影响运行
            {
                if (assetName.IndexOf("!a", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    action.Invoke(false, assetName, null);
                    return;
                }
            }

            var allAssetsHandle = await AssetComponent.LoadAllAssetsAsync(assetName);
            if (!allAssetsHandle.IsSucceed)
            {
                action.Invoke(false, assetName, null);
                return;
            }

            uiPackageData.SetResourceAllAssetsHandle(allAssetsHandle, assetName);
            if (uiPackageData.ResourceAllAssetsHandle == null)
            {
                action.Invoke(false, assetName, null);
                return;
            }

            string assetShortName = Path.GetFileNameWithoutExtension(assetName);
            foreach (var assetObject in uiPackageData.ResourceAllAssetsHandle.AllAssetObjects)
            {
                if (assetObject.name == assetShortName)
                {
                    switch (type)
                    {
                        case PackageItemType.Spine:
                        {
                            action.Invoke(true, assetName, assetObject as TextAsset);
                            break;
                        }

                        case PackageItemType.Atlas:
                        case PackageItemType.Image: //如果FGUI导出时没有选择分离通明通道，会因为加载不到!a结尾的Asset而报错，但是不影响运行
                        {
                            if (assetName.IndexOf("!a", StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                action.Invoke(false, assetName, null);
                                break;
                            }

                            action.Invoke(true, assetName, assetObject as Texture);
                            break;
                        }
                        case PackageItemType.Sound:
                        {
                            action.Invoke(true, assetName, assetObject as AudioClip);
                            break;
                        }
                        case PackageItemType.Font:
                        {
                            action.Invoke(true, assetName, assetObject as Font);
                        }
                            break;
//                 case PackageItemType.Spine:
//                 {
// #if FAIRYGUI_SPINE
//                     var assetHandle = await assetComponent.LoadAssetAsync<Spine.Unity.SkeletonDataAsset>(assetName);
//                     action.Invoke(assetHandle != null && assetHandle.AssetObject != null, assetName, assetHandle?.GetAssetObject<Spine.Unity.SkeletonDataAsset>());
// #else
//                             Log.Error("加载资源失败.暂未适配 Unknown file type: " + assetName + " extension: " + extension);
//                             action.Invoke(false, assetName, null);
// #endif
//                 }
                        // break;
                        case PackageItemType.DragoneBones:
                        {
#if FAIRYGUI_DRAGONBONES
                    var assetHandle = @await AssetComponent.LoadAssetAsync<DragonBones.DragonBonesData>(assetName);
                    action.Invoke(assetHandle != null && assetHandle.AssetObject != null, assetName, assetHandle?.GetAssetObject<DragonBones.DragonBonesData>());
#else
                            Log.Error("加载资源失败.暂未适配 Unknown file type: " + assetName + " extension: " + extension);
                            action.Invoke(false, assetName, null);
#endif
                        }
                            break;
                        default:
                        {
                            Log.Error("加载资源失败 Unknown file type: " + assetName + " extension: " + extension);
                            action.Invoke(false, assetName, null);
                            break;
                        }
                    }

                    return;
                }
            }

            Log.Error("加载资源失败 Unknown file type: " + assetName + " extension: " + extension);
            action.Invoke(false, assetName, null);
        }

        public void ReleaseResource(object obj)
        {
        }

        sealed class UIPackageData : IDisposable
        {
            /// <summary>
            /// 包名
            /// </summary>
            public readonly string PackageName;

            public void SetResourceAllAssetsHandle(AllAssetsHandle allAssetsHandle, string assetPath)
            {
                ResourceAllAssetsHandle = allAssetsHandle;
                ResourceAssetPath = assetPath;
            }

            /// <summary>
            /// 资源包
            /// </summary>
            public AllAssetsHandle ResourceAllAssetsHandle { get; private set; }

            /// <summary>
            /// 资源包资源路径
            /// </summary>
            public string ResourceAssetPath { get; private set; }

            public void SetDefiledAssetHandle(AssetHandle defiledAssetHandle, string defiledAssetPath)
            {
                DefiledAssetHandle = defiledAssetHandle;
                DefiledAssetPath = defiledAssetPath;
            }

            /// <summary>
            /// 描述文件包
            /// </summary>
            public AssetHandle DefiledAssetHandle { get; private set; }

            /// <summary>
            /// 描述文件包资源路径
            /// </summary>
            public string DefiledAssetPath { get; private set; }

            public UIPackageData(string packageName)
            {
                PackageName = packageName;
            }

            public void Dispose()
            {
                ResourceAllAssetsHandle?.Dispose();
                DefiledAssetHandle?.Dispose();
            }
        }
    }
}