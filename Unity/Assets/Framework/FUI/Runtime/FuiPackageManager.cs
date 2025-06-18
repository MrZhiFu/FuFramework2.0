using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 管理所有FUIPackage包。
    /// 1. 管理所有UI包的加载、卸载等操作。
    /// 2. 提供异步加载UI包的接口。
    /// </summary>
    public sealed class FuiPackageManager : GameFrameworkMonoSingleton<FuiPackageManager>
    {
        /// <summary>
        /// 表示UI包的数据结构。
        /// </summary>
        public sealed class UIPackageData
        {
            /// <summary>
            /// UI包的名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// UI包实例。
            /// </summary>
            public UIPackage Package { get; }

            /// <summary>
            /// 描述文件路径。
            /// </summary>
            public string DescFilePath { get; }

            /// <summary>
            /// 是否加载资源。
            /// </summary>
            public bool IsLoadAsset { get; }

            /// <summary>
            /// 初始化UI包数据结构。
            /// </summary>
            /// <param name="descFilePath">描述文件路径</param>
            /// <param name="isLoadAsset">是否加载资源</param>
            public UIPackageData(string descFilePath, bool isLoadAsset)
            {
                DescFilePath = descFilePath;
                Package      = UIPackage.AddPackage(descFilePath);
                IsLoadAsset  = isLoadAsset;
                Name         = Package.name;
            }
        }


        /// <summary>
        /// FUI异步资源加载器
        /// </summary>
        private FuiLoadAsyncResourceHelper m_ResourceHelper;

        /// <summary>
        /// 已加载的Package字典, Key为描述文件路径，Value为UI包的数据结构
        /// </summary>
        private readonly Dictionary<string, UIPackageData> m_UILoadedPkgDict = new(32);

        /// <summary>
        /// 正在加载的Package字典, Key为描述文件路径，Value为表示UI包加载过程的UniTaskCompletionSource
        /// </summary>
        private readonly Dictionary<string, UniTaskCompletionSource<UIPackage>> m_UIPackageLoading = new(32);


        private void Awake()
        {
            m_ResourceHelper = new FuiLoadAsyncResourceHelper();
            UIPackage.SetAsyncLoadResource(m_ResourceHelper);
        }

        /// <summary>
        /// 异步添加UI包(一般用于加载AssetBundle目录下的UI包)
        /// </summary>
        /// <param name="descFilePath">描述文件路径。</param>
        /// <param name="isLoadAsset">是否加载资源，默认为true。</param>
        /// <returns>返回一个表示UI包的UniTask。</returns>
        public UniTask<UIPackage> AddPackageAsync(string descFilePath, bool isLoadAsset = true)
        {
            if (m_UIPackageLoading.TryGetValue(descFilePath, out var tcsLoading))
                return tcsLoading.Task;

            if (m_UILoadedPkgDict.TryGetValue(descFilePath, out var packageData))
                return UniTask.FromResult(packageData.Package);

            var task = new UniTaskCompletionSource<UIPackage>();
            m_UIPackageLoading[descFilePath] = task;
            UIPackage.AddPackageAsync(descFilePath, Complete);
            return task.Task;

            // 加载完成回调
            void Complete(UIPackage uiPackage)
            {
                m_UIPackageLoading.Remove(descFilePath);
                packageData = new UIPackageData(descFilePath, isLoadAsset);

                if (isLoadAsset)
                    packageData.Package.LoadAllAssets();

                m_UILoadedPkgDict[descFilePath] = packageData;
                task.TrySetResult(packageData.Package);
            }
        }

        /// <summary>
        /// 同步添加UI包(一般用于加载Resource目录下的UI包)
        /// </summary>
        /// <param name="descFilePath">描述文件路径。</param>
        /// <param name="isLoadAsset">是否加载资源，默认为true。</param>
        public void AddPackageSync(string descFilePath, bool isLoadAsset = true)
        {
            if (m_UILoadedPkgDict.TryGetValue(descFilePath, out var packageData)) return;

            packageData = new UIPackageData(descFilePath, isLoadAsset);

            m_UILoadedPkgDict[descFilePath] = packageData;

            if (isLoadAsset)
                packageData.Package.LoadAllAssets();
        }

        /// <summary>
        /// 移除指定名称的UI包。
        /// </summary>
        /// <param name="packageName">UI包的名称。</param>
        public void RemovePackage(string packageName)
        {
            string        descPath      = null;
            UIPackageData uiPackageData = null;

            // 找到指定名称的UI包
            foreach (var (path, packageData) in m_UILoadedPkgDict)
            {
                if (!packageData.Name.EqualsFast(packageName)) continue;
                descPath      = path;
                uiPackageData = packageData;
                break;
            }

            if (uiPackageData == null) return;

            // 卸载包资源
            uiPackageData.Package.UnloadAssets();
            UIPackage.RemovePackage(packageName);
            m_ResourceHelper.ReleasePackage(packageName);
            m_UILoadedPkgDict.Remove(descPath);
        }

        /// <summary>
        /// 移除所有UI包。
        /// </summary>
        public void RemoveAllPackages()
        {
            var packages = UIPackage.GetPackages();
            foreach (var package in packages)
            {
                package.UnloadAssets();
            }

            m_ResourceHelper.ReleaseAllPackage();
            UIPackage.RemoveAllPackages();
            m_UILoadedPkgDict.Clear();
        }

        /// <summary>
        /// 检查是否存在指定名称的UI包。
        /// </summary>
        /// <param name="uiPackageName">UI包的名称。</param>
        /// <returns>如果存在返回true，否则返回false。</returns>
        public bool HasPackage(string uiPackageName) => GetPackage(uiPackageName) != null;

        /// <summary>
        /// 获取指定名称的UI包。
        /// </summary>
        /// <param name="uiPackageName">UI包的名称。</param>
        /// <returns>返回UI包实例，如果不存在则返回null。</returns>
        public UIPackage GetPackage(string uiPackageName)
        {
            foreach (var (_, uiPackageData) in m_UILoadedPkgDict)
            {
                if (uiPackageData.Name.EqualsFast(uiPackageName))
                {
                    return uiPackageData.Package;
                }
            }

            return null;
        }
    }
}