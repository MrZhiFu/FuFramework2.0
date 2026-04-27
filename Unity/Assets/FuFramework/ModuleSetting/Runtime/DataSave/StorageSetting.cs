using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 本地数据存储模块配置
    /// </summary>
    public class StorageSetting : ScriptableObject
    {
        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        [SerializeField] private bool m_EnableAutoSave = true;

        /// <summary>
        /// 自动保存间隔(秒, 默认5分钟)
        /// </summary>
        [SerializeField] private float m_AutoSaveInterval = 300f;

        /// <summary>
        /// 是否启用加密
        /// </summary>
        [SerializeField] private bool m_EnableEncrypt = false;

        /// <summary>
        /// 加密密钥
        /// </summary>
        [SerializeField] private string m_EncryptKey = "FuFrameworkStorageKey";


        /// <summary>
        /// 获取是否启用自动保存
        /// </summary>
        public bool EnableAutoSave => m_EnableAutoSave;

        /// <summary>
        /// 获取自动保存间隔(秒, 默认5分钟)
        /// </summary>
        public float AutoSaveInterval => m_AutoSaveInterval;

        /// <summary>
        /// 获取是否启用加密
        /// </summary>
        public bool EnableEncrypt => m_EnableEncrypt;

        /// <summary>
        /// 获取加密密钥
        /// </summary>
        public string EncryptKey => m_EncryptKey;

        /// <summary>
        /// 重置配置
        /// </summary>
        public void Reset()
        {
            m_EnableAutoSave   = true;
            m_AutoSaveInterval = 300f;
            m_EnableEncrypt    = false;
            m_EncryptKey       = "FuFrameworkStorageKey";
        }
    }
}