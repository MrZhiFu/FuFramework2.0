using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 本地数据存储模块配置
    /// </summary>
    public class DataSaveSetting : ScriptableObject
    {
        /// <summary>
        /// 资源下载最大并发数量
        /// </summary>
        [SerializeField] private bool m_EnableAutoSave = true;

        /// <summary>
        /// 自动保存间隔(秒, 默认5分钟)
        /// </summary>
        [SerializeField] private float m_AutoSaveInterval = 300f;

        
        /// <summary>
        /// 获取是否启用自动保存
        /// </summary>
        public bool EnableAutoSave => m_EnableAutoSave;

        /// <summary>
        /// 获取自动保存间隔(秒, 默认5分钟)
        /// </summary>
        public float AutoSaveInterval => m_AutoSaveInterval;
        
        /// <summary>
        /// 重置配置
        /// </summary>
        public void Reset()
        {
            m_EnableAutoSave   = true;
            m_AutoSaveInterval = 300f;
        }
    }
}