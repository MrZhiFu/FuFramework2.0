using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 资源模块配置
    /// </summary>
    public class AssetSetting : ScriptableObject
    {
        /// <summary>
        /// 资源运行模式
        /// </summary>
        [SerializeField] private EPlayMode m_PlayMode = EPlayMode.EditorSimulateMode;

        /// <summary>
        /// 资源包列表
        /// </summary>
        [SerializeField] private List<AssetPackageInfo> m_AssetPackages = new();

        /// <summary>
        /// 资源包字典，用于快速查找，key为包名称，value为资源包信息
        /// </summary>
        private Dictionary<string, AssetPackageInfo> m_PackageDictionary;

        /// <summary>
        /// 是否初始化完成
        /// </summary>
        private bool m_IsInitialized;

        /// <summary>
        /// 获取所有资源包
        /// </summary>
        public IReadOnlyList<AssetPackageInfo> AllPackages => m_AssetPackages;

        /// <summary>
        /// 资源包数量
        /// </summary>
        public int Count => m_AssetPackages.Count;

        /// <summary>
        /// 资源运行模式
        /// </summary>
        public EPlayMode PlayMode => m_PlayMode;

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        public AssetPackageInfo DefaultPackage
        {
            get
            {
                InitializeDictionary();
                return m_AssetPackages.FirstOrDefault(package => package.IsDefaultPackage);
            }
        }

        /// <summary>
        /// 索引器：通过名称获取资源包
        /// </summary>
        public AssetPackageInfo this[string packageName]
        {
            get
            {
                InitializeDictionary();
                return m_PackageDictionary.GetValueOrDefault(packageName);
            }
        }

        /// <summary>
        /// 索引器：通过索引获取资源包
        /// </summary>
        public AssetPackageInfo this[int index]
        {
            get
            {
                if (index >= 0 && index < m_AssetPackages.Count) return m_AssetPackages[index];
                return null;
            }
        }

        /// <summary>
        /// 通过名称获取资源包
        /// </summary>
        public AssetPackageInfo GetPackage(string packageName)
        {
            InitializeDictionary();
            return m_PackageDictionary.GetValueOrDefault(packageName);
        }

        /// <summary>
        /// 添加资源包
        /// </summary>
        public void AddPackage(AssetPackageInfo packageInfo)
        {
            if (packageInfo == null) return;

            InitializeDictionary();
            if (m_PackageDictionary.ContainsKey(packageInfo.PackageName)) return;

            // 如果添加的是默认包，确保只有一个默认包
            if (packageInfo.IsDefaultPackage)
            {
                SetAllPackagesNonDefault();
            }

            m_AssetPackages.Add(packageInfo);
            m_PackageDictionary[packageInfo.PackageName] = packageInfo;
        }

        /// <summary>
        /// 创建新的资源包
        /// </summary>
        public AssetPackageInfo CreateNewPackage(string packageName, bool isDefault = false)
        {
            // 确保名称唯一
            var uniqueName = GetUniqueName(packageName);
            var newPackage = new AssetPackageInfo(uniqueName);
            newPackage.IsDefaultPackage = isDefault;

            // 如果设置为默认包，取消其他包的默认状态
            if (isDefault)
            {
                SetAllPackagesNonDefault();
            }

            AddPackage(newPackage);
            return newPackage;
        }

        /// <summary>
        /// 添加默认资源包（如果不存在）
        /// </summary>
        public void AddDefaultPackage()
        {
            if (m_AssetPackages.Count == 0)
            {
                var defaultPackage = CreateNewPackage("DefaultPackage", true);
                defaultPackage.DownloadURL         = "http://default-server.com/assets";
                defaultPackage.FallbackDownloadURL = "http://fallback-server.com/assets";
            }
        }

        /// <summary>
        /// 设置指定包为默认包
        /// </summary>
        public bool SetDefaultPackage(string packageName)
        {
            InitializeDictionary();
            if (!m_PackageDictionary.TryGetValue(packageName, out var targetPackage)) return false;

            SetAllPackagesNonDefault();
            targetPackage.IsDefaultPackage = true;
            return true;
        }

        /// <summary>
        /// 移除资源包
        /// </summary>
        public void RemovePackage(AssetPackageInfo packageInfo)
        {
            if (packageInfo == null) return;

            InitializeDictionary();
            if (!m_PackageDictionary.ContainsKey(packageInfo.PackageName)) return;

            // 如果移除的是默认包，需要设置新的默认包
            bool wasDefault = packageInfo.IsDefaultPackage;

            m_AssetPackages.Remove(packageInfo);
            m_PackageDictionary.Remove(packageInfo.PackageName);

            // 如果移除了默认包且还有剩余包，设置第一个为默认
            if (wasDefault && m_AssetPackages.Count > 0)
            {
                m_AssetPackages[0].IsDefaultPackage = true;
            }
        }

        /// <summary>
        /// 移除指定名称的资源包
        /// </summary>
        public void RemovePackage(string packageName)
        {
            InitializeDictionary();
            if (!m_PackageDictionary.TryGetValue(packageName, out var package)) return;
            RemovePackage(package);
        }

        /// <summary>
        /// 移除指定索引的资源包
        /// </summary>
        public void RemovePackageAt(int index)
        {
            if (index < 0 || index >= m_AssetPackages.Count) return;
            var package = m_AssetPackages[index];
            RemovePackage(package);
        }

        /// <summary>
        /// 清空所有资源包
        /// </summary>
        public void ClearPackages()
        {
            m_AssetPackages.Clear();
            m_PackageDictionary?.Clear();
            m_IsInitialized = false;
        }

        /// <summary>
        /// 检查是否包含指定名称的资源包
        /// </summary>
        public bool ContainsPackage(string packageName)
        {
            InitializeDictionary();
            return m_PackageDictionary.ContainsKey(packageName);
        }

        /// <summary>
        /// 获取所有资源包的名称
        /// </summary>
        public List<string> GetAllPackageNames()
        {
            InitializeDictionary();
            return new List<string>(m_PackageDictionary.Keys);
        }

        /// <summary>
        /// 设置所有包为非默认状态
        /// </summary>
        private void SetAllPackagesNonDefault()
        {
            foreach (var package in m_AssetPackages)
            {
                package.IsDefaultPackage = false;
            }
        }

        /// <summary>
        /// 获取唯一名称
        /// </summary>
        private string GetUniqueName(string baseName)
        {
            var packageName = baseName;
            var counter     = 1;

            while (ContainsPackage(packageName))
            {
                packageName = $"{baseName} {counter}";
                counter++;
            }

            return packageName;
        }

        /// <summary>
        /// 初始化字典
        /// </summary>
        private void InitializeDictionary()
        {
            if (m_IsInitialized && m_PackageDictionary != null && m_PackageDictionary.Count == m_AssetPackages.Count) return;

            m_PackageDictionary = new Dictionary<string, AssetPackageInfo>();
            foreach (var package in m_AssetPackages.Where(package => package != null && !string.IsNullOrEmpty(package.PackageName)))
            {
                m_PackageDictionary.TryAdd(package.PackageName, package);
            }

            m_IsInitialized = true;
        }

        /// <summary>
        /// 在编辑器模式下验证数据
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            m_IsInitialized = false;
            InitializeDictionary();

            // 确保至少有一个默认包
            if (m_AssetPackages.Count > 0 && m_AssetPackages.All(p => !p.IsDefaultPackage))
            {
                m_AssetPackages[0].IsDefaultPackage = true;
            }
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            ClearPackages();
            AddDefaultPackage();
        }
    }
}