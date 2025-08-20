using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 音频模块配置
    /// </summary>
    public class SoundSetting : ScriptableObject
    {
        /// <summary>
        /// 音频混音器
        /// </summary>
        [SerializeField] private AudioMixer m_AudioMixer;

        /// <summary>
        /// 声音组列表
        /// </summary>
        [SerializeField] private List<SoundGroupInfo> m_SoundGroups = new();

        /// <summary>
        /// 声音组字典，用于快速查找，key为声音组名称，value为声音组
        /// </summary>
        private Dictionary<string, SoundGroupInfo> m_GroupDictionary;

        /// <summary>
        /// 是否初始化完成
        /// </summary>
        private bool m_IsInitialized;


        /// <summary>
        /// 获取所有声音组
        /// </summary>
        public IReadOnlyList<SoundGroupInfo> AllGroups => m_SoundGroups;

        /// <summary>
        /// 声音组数量
        /// </summary>
        public int Count => m_SoundGroups.Count;

        /// <summary>
        /// 音频混音器
        /// </summary>
        public AudioMixer AudioMixer => m_AudioMixer;

        /// <summary>
        /// 索引器：通过名称获取声音组
        /// </summary>
        public SoundGroupInfo this[string groupName]
        {
            get
            {
                InitializeDictionary();
                return m_GroupDictionary.GetValueOrDefault(groupName);
            }
        }

        /// <summary>
        /// 索引器：通过索引获取声音组
        /// </summary>
        public SoundGroupInfo this[int index]
        {
            get
            {
                if (index >= 0 && index < m_SoundGroups.Count) return m_SoundGroups[index];
                return null;
            }
        }

        /// <summary>
        /// 通过名称获取声音组
        /// </summary>
        public SoundGroupInfo GetGroup(string groupName)
        {
            InitializeDictionary();
            return m_GroupDictionary.GetValueOrDefault(groupName);
        }

        /// <summary>
        /// 通过ID获取声音组
        /// </summary>
        public SoundGroupInfo GetGroupByID(string groupID)
        {
            InitializeDictionary();
            return m_SoundGroups.FirstOrDefault(group => group.GroupID == groupID);
        }

        /// <summary>
        /// 添加声音组
        /// </summary>
        public void AddGroup(SoundGroupInfo groupInfo)
        {
            if (groupInfo == null) return;

            InitializeDictionary();
            if (m_GroupDictionary.ContainsKey(groupInfo.Name)) return;
            m_SoundGroups.Add(groupInfo);
            m_GroupDictionary[groupInfo.Name] = groupInfo;
        }

        /// <summary>
        /// 添加默认声音组
        /// </summary>
        public void AddDefaultSoundGroups()
        {
            string[] defaultGroups = { "BGM", "SFX", "UI" };

            foreach (var groupName in defaultGroups)
            {
                if (ContainsGroup(groupName)) continue;
                CreateNewSoundGroup(groupName);
            }
        }

        /// <summary>
        /// 创建新的声音组
        /// </summary>
        public SoundGroupInfo CreateNewSoundGroup(string groupName)
        {
            // 确保名称唯一
            var uniqueName = GetUniqueName(groupName);
            var newGroup   = new SoundGroupInfo(uniqueName);
            AddGroup(newGroup);
            return newGroup;
        }

        /// <summary>
        /// 移除声音组
        /// </summary>
        public void RemoveGroup(SoundGroupInfo groupInfo)
        {
            if (groupInfo == null) return;

            InitializeDictionary();
            if (!m_GroupDictionary.ContainsKey(groupInfo.Name)) return;
            m_SoundGroups.Remove(groupInfo);
            m_GroupDictionary.Remove(groupInfo.Name);
        }

        /// <summary>
        /// 移除指定名称的声音组
        /// </summary>
        public void RemoveGroup(string groupName)
        {
            InitializeDictionary();
            if (!m_GroupDictionary.TryGetValue(groupName, out var group)) return;
            m_SoundGroups.Remove(group);
            m_GroupDictionary.Remove(groupName);
        }

        /// <summary>
        /// 移除指定索引的声音组
        /// </summary>
        public void RemoveGroupAt(int index)
        {
            if (index < 0 || index >= m_SoundGroups.Count) return;
            var group = m_SoundGroups[index];
            RemoveGroup(group.Name);
        }

        /// <summary>
        /// 清空所有声音组
        /// </summary>
        public void ClearGroups()
        {
            m_SoundGroups.Clear();
            m_GroupDictionary?.Clear();
            m_IsInitialized = false;
        }

        /// <summary>
        /// 检查是否包含指定名称的声音组
        /// </summary>
        public bool ContainsGroup(string groupName)
        {
            InitializeDictionary();
            return m_GroupDictionary.ContainsKey(groupName);
        }

        /// <summary>
        /// 获取所有声音组的名称
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
            var counter   = 1;

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
            if (m_IsInitialized && m_GroupDictionary != null && m_GroupDictionary.Count == m_SoundGroups.Count) return;

            m_GroupDictionary = new Dictionary<string, SoundGroupInfo>();
            foreach (var group in m_SoundGroups.Where(group => group != null && !string.IsNullOrEmpty(group.Name)))
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
    }
}