using System.Linq;
using UnityEngine;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 实体模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "EntitySettings", menuName = "FuFramework/Entity Settings")]
    public class EntitySetting : ScriptableObject
    {
        /// <summary>
        /// 实体组列表
        /// </summary>
        [SerializeField] private List<EntityGroupInfo> m_EntityGroups = new();

        /// <summary>
        /// 实体组字典，用于快速查找，key为实体组名称，value为实体组
        /// </summary>
        private Dictionary<string, EntityGroupInfo> m_GroupDictionary;

        /// <summary>
        /// 是否初始化完成
        /// </summary>
        private bool m_IsInitialized;

        /// <summary>
        /// 获取所有实体组
        /// </summary>
        public IReadOnlyList<EntityGroupInfo> AllGroups => m_EntityGroups;

        /// <summary>
        /// 实体组数量
        /// </summary>
        public int Count => m_EntityGroups.Count;

        /// <summary>
        /// 索引器：通过名称获取实体组
        /// </summary>
        public EntityGroupInfo this[string groupName]
        {
            get
            {
                InitializeDictionary();
                return m_GroupDictionary.GetValueOrDefault(groupName);
            }
        }

        /// <summary>
        /// 索引器：通过索引获取实体组
        /// </summary>
        public EntityGroupInfo this[int index]
        {
            get
            {
                if (index >= 0 && index < m_EntityGroups.Count) return m_EntityGroups[index];
                return null;
            }
        }

        /// <summary>
        /// 通过名称获取实体组
        /// </summary>
        public EntityGroupInfo GetGroup(string groupName)
        {
            InitializeDictionary();
            return m_GroupDictionary.GetValueOrDefault(groupName);
        }

        /// <summary>
        /// 通过ID获取实体组
        /// </summary>
        public EntityGroupInfo GetGroupByID(string groupID)
        {
            InitializeDictionary();
            return m_EntityGroups.FirstOrDefault(group => group.GroupID == groupID);
        }

        /// <summary>
        /// 添加实体组
        /// </summary>
        public void AddGroup(EntityGroupInfo groupInfo)
        {
            if (groupInfo == null) return;

            InitializeDictionary();
            if (m_GroupDictionary.ContainsKey(groupInfo.Name)) return;
            m_EntityGroups.Add(groupInfo);
            m_GroupDictionary[groupInfo.Name] = groupInfo;
        }

        /// <summary>
        /// 添加默认实体组
        /// </summary>
        public void AddDefaultEntityGroups()
        {
            string[] defaultGroups = { "Player", "Enemy", "NPC", "Prop", "Effect" };

            foreach (var groupName in defaultGroups)
            {
                if (ContainsGroup(groupName)) continue;
                CreateNewEntityGroup(groupName);
            }
        }

        /// <summary>
        /// 创建新的实体组
        /// </summary>
        public EntityGroupInfo CreateNewEntityGroup(string groupName)
        {
            // 确保名称唯一
            var uniqueName = GetUniqueName(groupName);
            var newGroup = new EntityGroupInfo(uniqueName);
            AddGroup(newGroup);
            return newGroup;
        }

        /// <summary>
        /// 移除实体组
        /// </summary>
        public void RemoveGroup(EntityGroupInfo groupInfo)
        {
            if (groupInfo == null) return;

            InitializeDictionary();
            if (!m_GroupDictionary.ContainsKey(groupInfo.Name)) return;
            m_EntityGroups.Remove(groupInfo);
            m_GroupDictionary.Remove(groupInfo.Name);
        }

        /// <summary>
        /// 移除指定名称的实体组
        /// </summary>
        public void RemoveGroup(string groupName)
        {
            InitializeDictionary();
            if (!m_GroupDictionary.TryGetValue(groupName, out var group)) return;
            m_EntityGroups.Remove(group);
            m_GroupDictionary.Remove(groupName);
        }

        /// <summary>
        /// 移除指定索引的实体组
        /// </summary>
        public void RemoveGroupAt(int index)
        {
            if (index < 0 || index >= m_EntityGroups.Count) return;
            var group = m_EntityGroups[index];
            RemoveGroup(group.Name);
        }

        /// <summary>
        /// 清空所有实体组
        /// </summary>
        public void ClearGroups()
        {
            m_EntityGroups.Clear();
            m_GroupDictionary?.Clear();
            m_IsInitialized = false;
        }

        /// <summary>
        /// 检查是否包含指定名称的实体组
        /// </summary>
        public bool ContainsGroup(string groupName)
        {
            InitializeDictionary();
            return m_GroupDictionary.ContainsKey(groupName);
        }

        /// <summary>
        /// 获取所有实体组的名称
        /// </summary>
        public List<string> GetAllGroupNames()
        {
            InitializeDictionary();
            return new List<string>(m_GroupDictionary.Keys);
        }

        /// <summary>
        /// 获取唯一名称
        /// </summary>
        private string GetUniqueName(string baseName)
        {
            var groupName = baseName;
            var counter = 1;

            while (ContainsGroup(groupName))
            {
                groupName = $"{baseName} {counter}";
                counter++;
            }

            return groupName;
        }

        /// <summary>
        /// 初始化字典
        /// </summary>
        private void InitializeDictionary()
        {
            if (m_IsInitialized && m_GroupDictionary != null && m_GroupDictionary.Count == m_EntityGroups.Count) return;

            m_GroupDictionary = new Dictionary<string, EntityGroupInfo>();
            foreach (var group in m_EntityGroups.Where(group => group != null && !string.IsNullOrEmpty(group.Name)))
            {
                m_GroupDictionary.TryAdd(group.Name, group);
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
        }

        /// <summary>
        /// 设置所有实体组的实例容量
        /// </summary>
        public void SetAllGroupsInstanceCapacity(int capacity)
        {
            capacity = Mathf.Max(0, capacity);
            foreach (var group in m_EntityGroups)
            {
                group.InstanceCapacity = capacity;
            }
        }

        /// <summary>
        /// 设置所有实体组的实例过期时间
        /// </summary>
        public void SetAllGroupsInstanceExpireTime(float expireTime)
        {
            expireTime = Mathf.Max(0f, expireTime);
            foreach (var group in m_EntityGroups)
            {
                group.InstanceExpireTime = expireTime;
            }
        }

        /// <summary>
        /// 设置所有实体组的实例自动释放间隔
        /// </summary>
        public void SetAllGroupsAutoReleaseInterval(float interval)
        {
            interval = Mathf.Max(0f, interval);
            foreach (var group in m_EntityGroups)
            {
                group.InstanceAutoReleaseInterval = interval;
            }
        }
    }
}