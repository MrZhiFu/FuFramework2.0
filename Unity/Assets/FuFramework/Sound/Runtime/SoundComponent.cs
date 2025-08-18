using UnityEngine;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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


        [Header("声音默认辅助器类型名")]
        [SerializeField] private string m_SoundHelperTypeName = "FuFramework.Sound.Runtime.DefaultSoundHelper";

        [Header("自定义声音辅助器")]
        [SerializeField] private SoundHelperBase m_CustomSoundHelper;

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
                Log.Fatal("Sound manager is invalid.");
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
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (!m_EventComponent)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            // 设置资源管理器
            m_SoundManager.SetResourceManager(FuEntry.GetModule<IAssetManager>());

            // 设置声音辅助器
            SoundHelperBase soundHelper = Helper.CreateHelper(m_SoundHelperTypeName, m_CustomSoundHelper);
            if (!soundHelper)
            {
                Log.Error("Can not create sound helper.");
                return;
            }

            soundHelper.name = "Sound Helper";
            soundHelper.transform.SetParent(transform);
            soundHelper.transform.localScale = Vector3.one;

            m_SoundManager.SetSoundHelper(soundHelper);

            if (!m_InstanceRoot)
            {
                m_InstanceRoot = new GameObject("Sound Instances").transform;
                m_InstanceRoot.SetParent(transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            // 添加声音组
            foreach (var group in m_SoundGroups)
            {
                if (AddSoundGroup(group.Name, group.AvoidBeingReplacedBySamePriority, group.Mute, group.Volume, group.AgentHelperCount)) continue;
                Log.Warning("Add sound group '{0}' failure.", group.Name);
            }
        }

        private void OnDestroy()
        {
#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded   -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
#endif
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
        public ISoundGroup GetSoundGroup(string soundGroupName) => m_SoundManager.GetSoundGroup(soundGroupName);

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <returns>所有声音组。</returns>
        public ISoundGroup[] GetAllSoundGroups() => m_SoundManager.GetAllSoundGroups();

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <param name="results">所有声音组。</param>
        public void GetAllSoundGroups(List<ISoundGroup> results) => m_SoundManager.GetAllSoundGroups(results);

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundAgentHelperCount">声音代理辅助器数量。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string soundGroupName, int soundAgentHelperCount)
        {
            return AddSoundGroup(soundGroupName, false, false, 1f, soundAgentHelperCount);
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundGroupAvoidBeingReplacedBySamePriority">声音组中的声音是否避免被同优先级声音替换。</param>
        /// <param name="soundGroupMute">声音组是否静音。</param>
        /// <param name="soundGroupVolume">声音组音量。</param>
        /// <param name="soundAgentHelperCount">声音代理辅助器数量。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string soundGroupName, bool soundGroupAvoidBeingReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume, int soundAgentHelperCount)
        {
            if (m_SoundManager.HasSoundGroup(soundGroupName)) return false;

            SoundGroupHelperBase soundGroupHelper = Helper.CreateHelper(m_SoundGroupHelperTypeName, m_CustomSoundGroupHelper, SoundGroupCount);
            if (!soundGroupHelper)
            {
                Log.Error("Can not create sound group helper.");
                return false;
            }

            soundGroupHelper.name = Utility.Text.Format("Sound Group - {0}", soundGroupName);
            soundGroupHelper.transform.SetParent(m_InstanceRoot);
            soundGroupHelper.transform.localScale = Vector3.one;

            if (m_AudioMixer)
            {
                // 设置声音组的混音器
                AudioMixerGroup[] audioMixerGroups = m_AudioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}", soundGroupName));
                if (audioMixerGroups.Length > 0)
                    soundGroupHelper.AudioMixerGroup = audioMixerGroups[0];
                else
                    soundGroupHelper.AudioMixerGroup = m_AudioMixer.FindMatchingGroups("Master")[0];
            }

            if (!m_SoundManager.AddSoundGroup(soundGroupName, soundGroupAvoidBeingReplacedBySamePriority, soundGroupMute, soundGroupVolume, soundGroupHelper))
                return false;

            for (var i = 0; i < soundAgentHelperCount; i++)
            {
                if (AddSoundAgentHelper(soundGroupName, soundGroupHelper, i)) continue;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 增加声音代理辅助器。
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
                Log.Error("Can not create sound agent helper.");
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
        /// 播放声音。
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
            return await m_SoundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, PlaySoundInfo.Create(bindingEntity, Vector3.zero, userData), serialId);
        }

        /// <summary>
        /// 播放声音。
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
            => m_SoundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, PlaySoundInfo.Create(null, worldPosition, userData), -1);

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

        #region 暂停播放声音

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

        #endregion

        #region 继续播放声音

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
            if (eventArgs.UserData is PlaySoundInfo playSoundInfo)
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
            var logMessage = Utility.Text.Format("Play sound failure, asset name '{0}', sound group name '{1}', error code '{2}', error message '{3}'.",
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