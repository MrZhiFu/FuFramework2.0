using UnityEngine;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 声音组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Sound")]
    public sealed partial class SoundComponent : FuComponent
    {
        private const int DefaultPriority = 0; // 模块默认优先级

        private ISoundManager  m_SoundManager;   // 声音管理器
        private EventComponent m_EventComponent; // 事件组件
        private AudioListener  m_AudioListener;  // 声音监听器

        [Header("Sound实例根节点")]
        [SerializeField] private Transform m_InstanceRoot;

        [Header("混音器")]
        [SerializeField] private AudioMixer m_AudioMixer;

        [Header("声音组默认辅助器类型名")]
        [SerializeField] private string m_SoundGroupHelperTypeName = "FuFramework.Sound.Runtime.DefaultSoundGroupHelper";

        [Header("自定义声音组辅助器")]
        [SerializeField] private SoundGroupHelperBase m_CustomSoundGroupHelper;

        [Header("声音代理默认辅助器类型名")]
        [SerializeField] private string m_SoundAgentHelperTypeName = "FuFramework.Sound.Runtime.DefaultSoundAgentHelper";

        [Header("自定义声音代理默认辅助器类型")]
        [SerializeField] private SoundAgentHelperBase m_CustomSoundAgentHelper;

        [Header("所有声音组")]
        [SerializeField] private SoundGroup[] m_SoundGroups;

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => m_SoundManager.SoundGroupCount;

        /// <summary>
        /// 获取声音混响器。
        /// </summary>
        public AudioMixer AudioMixer => m_AudioMixer;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(ISoundManager);
            base.Awake();
            m_SoundManager = FuEntry.GetModule<ISoundManager>();
            if (m_SoundManager == null)
            {
                Log.Fatal("[SoundComponent] 声音管理器不存在!");
                return;
            }

            m_SoundManager.PlaySoundSuccess += OnPlaySoundSuccess;
            m_SoundManager.PlaySoundFailure += OnPlaySoundFailure;

            m_AudioListener = gameObject.GetOrAddComponent<AudioListener>();

            SceneManager.sceneLoaded   += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Start()
        {
            var baseComp = GameEntry.GetComponent<BaseComponent>();
            if (!baseComp)
            {
                Log.Fatal("[SoundComponent] Base组件不存在!");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (!m_EventComponent)
            {
                Log.Fatal("[SoundComponent] 事件组件不存在!");
                return;
            }

            // 设置资源管理器
            m_SoundManager.SetResourceManager(FuEntry.GetModule<IAssetManager>());

            if (!m_InstanceRoot)
            {
                m_InstanceRoot = new GameObject("Sound Instances").transform;
                m_InstanceRoot.SetParent(transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            // 添加声音组
            foreach (var group in m_SoundGroups)
            {
                if (AddSoundGroup(group.Name, group.AllowBeingReplacedBySamePriority, group.Mute, group.Volume, group.AgentHelperCount)) continue;
                Log.Warning("[SoundComponent] 添加声音组 '{0}' 失败!", group.Name);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded   -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        #region 声音组

        /// <summary>
        /// 是否存在指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>指定声音组是否存在。</returns>
        public bool HasSoundGroup(string soundGroupName) => m_SoundManager.HasSoundGroup(soundGroupName);

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public SoundManager.SoundGroup GetSoundGroup(string soundGroupName) => m_SoundManager.GetSoundGroup(soundGroupName);

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <returns>所有声音组。</returns>
        public SoundManager.SoundGroup[] GetAllSoundGroups() => m_SoundManager.GetAllSoundGroups();

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <param name="results">所有声音组。</param>
        public void GetAllSoundGroups(List<SoundManager.SoundGroup> results) => m_SoundManager.GetAllSoundGroups(results);

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="soundAgentHelperCount">声音代理辅助器数量。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string groupName, int soundAgentHelperCount)
        {
            return AddSoundGroup(groupName, false, false, 1f, soundAgentHelperCount);
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="allowBeReplacedBySamePriority">声音组中的声音是否允许被同优先级声音替换。</param>
        /// <param name="groupMute">声音组是否静音。</param>
        /// <param name="groupVolume">声音组音量。</param>
        /// <param name="soundAgentHelperCount">声音代理辅助器数量。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string groupName, bool allowBeReplacedBySamePriority, bool groupMute, float groupVolume, int soundAgentHelperCount)
        {
            if (m_SoundManager.HasSoundGroup(groupName)) return false;

            SoundGroupHelperBase soundGroupHelper = Helper.CreateHelper(m_SoundGroupHelperTypeName, m_CustomSoundGroupHelper, SoundGroupCount);
            if (!soundGroupHelper)
            {
                Log.Error("[SoundComponent] 创建声音组辅助器失败!.");
                return false;
            }

            soundGroupHelper.name = Utility.Text.Format("Sound Group - {0}", groupName);
            soundGroupHelper.transform.SetParent(m_InstanceRoot);
            soundGroupHelper.transform.localScale = Vector3.one;

            if (m_AudioMixer)
            {
                // 设置声音组辅助器所在的混音组。
                AudioMixerGroup[] audioMixerGroups = m_AudioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}", groupName));
                if (audioMixerGroups.Length > 0)
                    soundGroupHelper.AudioMixerGroup = audioMixerGroups[0];
                else
                    soundGroupHelper.AudioMixerGroup = m_AudioMixer.FindMatchingGroups("Master")[0];
            }

            if (!m_SoundManager.AddSoundGroup(groupName, allowBeReplacedBySamePriority, groupMute, groupVolume, soundGroupHelper))
                return false;

            // 添加声音组辅助器中的声音播放代理辅助器
            for (var i = 0; i < soundAgentHelperCount; i++)
            {
                if (AddSoundAgentHelper(groupName, soundGroupHelper, i)) continue;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 增加声音代理辅助器到声音组下。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundGroupHelper">声音组辅助器。</param>
        /// <param name="index">声音代理辅助器索引。</param>
        /// <returns>是否增加声音代理辅助器成功。</returns>
        private bool AddSoundAgentHelper(string soundGroupName, SoundGroupHelperBase soundGroupHelper, int index)
        {
            SoundAgentHelperBase soundAgentHelper = Helper.CreateHelper(m_SoundAgentHelperTypeName, m_CustomSoundAgentHelper, index);
            if (!soundAgentHelper)
            {
                Log.Error("[SoundComponent] 创建声音代理辅助器失败!");
                return false;
            }

            soundAgentHelper.name = Utility.Text.Format("Sound Agent Helper - {0} - {1}", soundGroupName, index);
            soundAgentHelper.transform.SetParent(soundGroupHelper.transform);
            soundAgentHelper.transform.localScale = Vector3.one;

            if (m_AudioMixer)
            {
                AudioMixerGroup[] audioMixerGroups = m_AudioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}/{1}", soundGroupName, index));
                if (audioMixerGroups.Length > 0)
                    soundAgentHelper.AudioMixerGroup = audioMixerGroups[0];
                else
                    soundAgentHelper.AudioMixerGroup = soundGroupHelper.AudioMixerGroup;
            }

            m_SoundManager.AddSoundAgentHelper(soundGroupName, soundAgentHelper);

            return true;
        }

        #endregion

        #region 声音Get方法

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <returns>所有正在加载声音的序列编号。</returns>
        public int[] GetAllLoadingSoundSerialIds() => m_SoundManager.GetAllLoadingSoundSerialIds();

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载声音的序列编号。</param>
        public void GetAllLoadingSoundSerialIds(List<int> results) => m_SoundManager.GetAllLoadingSoundSerialIds(results);

        /// <summary>
        /// 是否正在加载声音。
        /// </summary>
        /// <param name="serialId">声音序列编号。</param>
        /// <returns>是否正在加载声音。</returns>
        public bool IsLoadingSound(int serialId) => m_SoundManager.IsLoadingSound(serialId);

        #endregion

        #region 播放声音

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, null, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority)
            => PlaySound(soundAssetName, soundGroupName, priority, null, null, null, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="serialId">序列编号。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySoundBySerialId(string soundAssetName, string soundGroupName, int serialId)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, null, serialId);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="serialId">序列编号。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySoundBySerialId(string soundAssetName, string soundGroupName, int serialId, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, playSoundParams, null, null, serialId);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, playSoundParams, null, null, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, Entity.Runtime.Entity bindingEntity)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, bindingEntity, null, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, Vector3 worldPosition)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, worldPosition, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, object userData)
            => PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, userData, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null, null, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, object userData)
            => PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null, userData, -1);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity.Runtime.Entity bindingEntity)
            => await PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, bindingEntity, null, -1);

        /// <summary>
        /// 播放声音
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="serialId">序列编号</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity.Runtime.Entity bindingEntity, object userData,
                                            int serialId)
        {
            var playSoundInfoExtra = PlaySoundInfoExtra.Create(bindingEntity, Vector3.zero, userData);
            return await m_SoundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, playSoundInfoExtra, serialId);
        }

        /// <summary>
        /// 播放声音
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Vector3 worldPosition)
            => PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, worldPosition, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Vector3 worldPosition, object userData)
        {
            var playSoundInfoExtra = PlaySoundInfoExtra.Create(null, worldPosition, userData);
            return m_SoundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, playSoundInfoExtra, -1);
        }

        #endregion

        #region 停止播放声音

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId) => m_SoundManager.StopSound(serialId);

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId, float fadeOutSeconds) => m_SoundManager.StopSound(serialId, fadeOutSeconds);

        /// <summary>
        /// 停止所有已加载的声音。
        /// </summary>
        public void StopAllLoadedSounds() => m_SoundManager.StopAllLoadedSounds();

        /// <summary>
        /// 停止所有已加载的声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public void StopAllLoadedSounds(float fadeOutSeconds) => m_SoundManager.StopAllLoadedSounds(fadeOutSeconds);

        /// <summary>
        /// 停止所有正在加载的声音。
        /// </summary>
        public void StopAllLoadingSounds() => m_SoundManager.StopAllLoadingSounds();

        #endregion

        #region 暂停/播放声音

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="serialId">要暂停播放声音的序列编号。</param>
        public void PauseSound(int serialId) => m_SoundManager.PauseSound(serialId);

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="serialId">要暂停播放声音的序列编号。</param>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public void PauseSound(int serialId, float fadeOutSeconds) => m_SoundManager.PauseSound(serialId, fadeOutSeconds);

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="serialId">要恢复播放声音的序列编号。</param>
        public void ResumeSound(int serialId) => m_SoundManager.ResumeSound(serialId);

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="serialId">要恢复播放声音的序列编号。</param>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public void ResumeSound(int serialId, float fadeInSeconds) => m_SoundManager.ResumeSound(serialId, fadeInSeconds);

        #endregion

        #region 事件处理

        /// <summary>
        /// 声音播放成功事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnPlaySoundSuccess(object sender, PlaySoundSuccessEventArgs eventArgs)
        {
            if (eventArgs.UserData is PlaySoundInfoExtra playSoundInfo)
            {
                var soundAgentHelper = eventArgs.SoundAgent.Helper as SoundAgentHelperBase;
                if (!soundAgentHelper) return;

                if (playSoundInfo.BindingEntity)
                    soundAgentHelper.SetBindingEntity(playSoundInfo.BindingEntity);
                else
                    soundAgentHelper.SetWorldPosition(playSoundInfo.WorldPosition);
            }

            m_EventComponent.Fire(this, eventArgs);
        }

        /// <summary>
        /// 声音播放失败事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnPlaySoundFailure(object sender, PlaySoundFailureEventArgs eventArgs)
        {
            var logMessage = Utility.Text.Format("[SoundComponent]播放声音 '{0}' 失败, 声音组 '{1}', 错误类型 '{2}', 错误信息 '{3}'.",
                                                 eventArgs.SoundAssetName, eventArgs.SoundGroupName, eventArgs.ErrorCode, eventArgs.ErrorMessage);
            if (eventArgs.ErrorCode == PlaySoundErrorCode.IgnoredDueToLowPriority)
                Log.Info(logMessage);
            else
                Log.Warning(logMessage);

            m_EventComponent.Fire(this, eventArgs);
        }


        /// <summary>
        /// 场景加载成功时刷新AudioListener。
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="loadSceneMode"></param>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode) => RefreshAudioListener();

        /// <summary>
        /// 场景卸载时刷新AudioListener。
        /// </summary>
        /// <param name="scene"></param>
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene) => RefreshAudioListener();

        /// <summary>
        /// 刷新AudioListener。
        /// </summary>
        private void RefreshAudioListener() => m_AudioListener.enabled = FindObjectsOfType<AudioListener>().Length <= 1;

        #endregion
    }
}