// using System;
// using System.Collections.Generic;
// using System.IO;
// using FairyGUI;
// using GameFrameX.Asset.Runtime;
// using GameFrameX.Runtime;
// using UnityEngine;
// using YooAsset;
//
// namespace GameFrameX.UI.FairyGUI.Runtime
// {
//     /// <summary>
//     /// FUI异步加载资源辅助器。
//     /// 1.实现FairyGUI的异步加载资源接口，使用YooAsset异步加载FUI中各种类型的资源
//     /// </summary>
//     
//     internal sealed class FuiLoadAsyncResourceHelper : IAsyncResource
//     {
//         /// <summary>
//         /// UI包数据
//         /// </summary>
//         private sealed class UIPackageData : IDisposable
//         {
//             /// <summary>
//             /// 包名
//             /// </summary>
//             public readonly string PackageName;
//
//             /// <summary>
//             /// 描述文件包资源路径
//             /// </summary>
//             public string DescAssetPath { get; private set; }
//
//             /// <summary>
//             /// 资源包资源路径
//             /// </summary>
//             public string ResourceAssetPath { get; private set; }
//
//             /// <summary>
//             /// 描述文件包desc资源处理器
//             /// </summary>
//             public AssetHandle DescAssetHandle { get; private set; }
//
//             /// <summary>
//             /// 资源包处理器
//             /// </summary>
//             public AllAssetsHandle ResourceAllAssetsHandle { get; private set; }
//
//             /// <summary>
//             /// 构造函数
//             /// </summary>
//             /// <param name="packageName"></param>
//             public UIPackageData(string packageName)
//             {
//                 PackageName = packageName;
//             }
//             
//             /// <summary>
//             /// 设置描述文件包资源处理器
//             /// </summary>
//             /// <param name="defiledAssetHandle"></param>
//             /// <param name="defiledAssetPath"></param>
//             public void SetDescAssetHandle(AssetHandle defiledAssetHandle, string defiledAssetPath)
//             {
//                 DescAssetHandle = defiledAssetHandle;
//                 DescAssetPath = defiledAssetPath;
//             }
//
//             /// <summary>
//             /// 设置资源包处理器
//             /// </summary>
//             /// <param name="allAssetsHandle"></param>
//             /// <param name="assetPath"></param>
//             public void SetResourceAllAssetsHandle(AllAssetsHandle allAssetsHandle, string assetPath)
//             {
//                 ResourceAllAssetsHandle = allAssetsHandle;
//                 ResourceAssetPath = assetPath;
//             }
//
//             /// <summary>
//             /// 释放资源
//             /// </summary>
//             public void Dispose()
//             {
//                 ResourceAllAssetsHandle?.Dispose();
//                 DescAssetHandle?.Dispose();
//             }
//         }
//
//         /// <summary>
//         /// UI包数据字典，key为UI包名称，value为UI包数据
//         /// </summary>
//         private readonly Dictionary<string, UIPackageData> m_UIPackageDict = new(32);
//
//         /// <summary>
//         /// 资源组件
//         /// </summary>
//         private AssetComponent m_assetComponent;
//
//         /// <summary>
//         /// 资源组件
//         /// </summary>
//         private AssetComponent AssetComponent
//         {
//             get
//             {
//                 if (m_assetComponent == null)
//                     m_assetComponent = GameEntry.GetComponent<AssetComponent>();
//                 return m_assetComponent;
//             }
//         }
//
//         /// <summary>
//         /// 实现FairyGUI的异步加载资源接口，使用YooAsset异步加载资源
//         /// </summary>
//         /// <param name="assetName">资源名称</param>
//         /// <param name="uiPackageName">UI包名称</param>
//         /// <param name="extension">扩展名</param>
//         /// <param name="type">资源类型</param>
//         /// <param name="loadCompleteCallBack">加载完成回调，参数为是否成功，资源名称，资源对象</param>
//         public async void LoadResource(string assetName, string uiPackageName, string extension, PackageItemType type,
//             Action<bool, string, object> loadCompleteCallBack)
//         {
//             try
//             {
//                 if (!m_UIPackageDict.TryGetValue(uiPackageName, out var uiPackageData))
//                 {
//                     uiPackageData = new UIPackageData(uiPackageName);
//                     m_UIPackageDict.Add(uiPackageName, uiPackageData);
//                 }
//
//                 // 描述文件
//                 if (type == PackageItemType.Misc)
//                 {
//                     AssetHandle assetHandle;
//                     if (uiPackageData.DescAssetHandle == null)
//                     {
//                         // 等待描述文件加载完成，并设置描述文件包资源处理器
//                         assetHandle = await AssetComponent.LoadAssetAsync(assetName);
//                         uiPackageData.SetDescAssetHandle(assetHandle, assetName);
//                     }
//                     else
//                     {
//                         assetHandle = uiPackageData.DescAssetHandle;
//                     }
//
//                     var isSuccess = assetHandle != null && assetHandle.AssetObject != null;
//                     loadCompleteCallBack.Invoke(isSuccess, assetName, assetHandle?.GetAssetObject<TextAsset>());
//                     return;
//                 }
//
//                 // 其他资源文件
//                 // 如果FGUI导出时没有选择分离通明通道，会因为加载不到!a结尾的Asset而报错，但是不影响运行
//                 if (type is PackageItemType.Image or PackageItemType.Atlas)
//                 {
//                     if (assetName.IndexOf("!a", StringComparison.OrdinalIgnoreCase) > -1)
//                     {
//                         loadCompleteCallBack.Invoke(false, assetName, null);
//                         return;
//                     }
//                 }
//
//                 // 等待资源包加载完成
//                 var allAssetsHandle = await AssetComponent.LoadAllAssetsAsync(assetName);
//                 if (!allAssetsHandle.IsSucceed)
//                 {
//                     loadCompleteCallBack.Invoke(false, assetName, null);
//                     return;
//                 }
//                 
//                 // 设置资源包处理器
//                 uiPackageData.SetResourceAllAssetsHandle(allAssetsHandle, assetName);
//                 if (uiPackageData.ResourceAllAssetsHandle == null)
//                 {
//                     loadCompleteCallBack.Invoke(false, assetName, null);
//                     return;
//                 }
//
//                 // 遍历资源包的加载处理器的资源对象列表，找到名称匹配的资源对象
//                 var assetShortName = Path.GetFileNameWithoutExtension(assetName);
//                 foreach (var assetObject in uiPackageData.ResourceAllAssetsHandle.AllAssetObjects)
//                 {
//                     if (assetObject.name != assetShortName) continue;
//                     
//                     switch (type)
//                     {
//                         case PackageItemType.Spine:
//                             loadCompleteCallBack.Invoke(true, assetName, assetObject as TextAsset);
//                             break;
//                         case PackageItemType.Atlas:
//                         case PackageItemType.Image:
//                             //如果FGUI导出时没有选择分离通明通道，会因为加载不到!a结尾的Asset而报错，但是不影响运行
//                             if (assetName.IndexOf("!a", StringComparison.OrdinalIgnoreCase) > -1)
//                             {
//                                 loadCompleteCallBack.Invoke(false, assetName, null);
//                                 break;
//                             }
//
//                             loadCompleteCallBack.Invoke(true, assetName, assetObject as Texture);
//                             break;
//                         case PackageItemType.Sound:
//                             loadCompleteCallBack.Invoke(true, assetName, assetObject as AudioClip);
//                             break;
//                         case PackageItemType.Font:
//                             loadCompleteCallBack.Invoke(true, assetName, assetObject as Font);
//                             break;
//                         case PackageItemType.DragoneBones:
// #if FAIRYGUI_DRAGONBONES
//                             var assetHandle = @await AssetComponent.LoadAssetAsync<DragonBones.DragonBonesData>(assetName);
//                             action.Invoke(assetHandle != null && assetHandle.AssetObject != null, assetName, assetHandle?.GetAssetObject<DragonBones.DragonBonesData>());
// #else
//                             Log.Error("加载资源失败.暂未适配 Unknown file type: " + assetName + " extension: " + extension);
//                             loadCompleteCallBack.Invoke(false, assetName, null);
// #endif
//                             break;
//                         default:
//                             Log.Error("加载资源失败 Unknown file type: " + assetName + " extension: " + extension);
//                             loadCompleteCallBack.Invoke(false, assetName, null);
//                             break;
//                     }
//
//                     return;
//                 }
//
//                 Log.Error("加载资源失败 Unknown file type: " + assetName + " extension: " + extension);
//                 loadCompleteCallBack.Invoke(false, assetName, null);
//             }
//             catch (Exception e)
//             {
//                 Log.Error(e);
//             }
//         }
//
//         /// <summary>
//         /// 实现FairyGUI的释放资源接口，这里不做任何处理
//         /// </summary>
//         /// <param name="obj"></param>
//         public void ReleaseResource(object obj) { }
//
//         /// <summary>
//         /// 释放指定UI包
//         /// </summary>
//         /// <param name="uiPackageName"></param>
//         public void ReleasePackage(string uiPackageName)
//         {
//             if (!m_UIPackageDict.TryGetValue(uiPackageName, out var uiPackageData)) return;
//             AssetComponent.UnloadAsset(uiPackageData.DescAssetPath);
//             AssetComponent.UnloadAsset(uiPackageData.ResourceAssetPath);
//             uiPackageData.Dispose();
//             m_UIPackageDict.Remove(uiPackageName);
//         }
//
//         /// <summary>
//         /// 释放所有UI包
//         /// </summary>
//         public void ReleaseAllPackage()
//         {
//             foreach (var (_, packageData) in m_UIPackageDict)
//             {
//                 AssetComponent.UnloadAsset(packageData.DescAssetPath);
//                 AssetComponent.UnloadAsset(packageData.ResourceAssetPath);
//                 packageData.Dispose();
//             }
//
//             m_UIPackageDict.Clear();
//         }
//     }
// }