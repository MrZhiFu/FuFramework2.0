using System;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
using FuFramework.Event.Runtime;
using FuFramework.ModuleSetting.Runtime;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Utility = FuFramework.Core.Runtime.Utility;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 声音管理器。
    /// 功能：实现了声音管理器相关接口，包括声音组、声音播放，暂停，继续，停止等。
    /// </summary>
    public sealed partial class SoundManager : MonoSingleton<SoundManager>
    {
        private const int DefaultPriority = 0; // 模块默认优先级

        private readonly Dictionary<string, SoundGroup> m_SoundGroupDict = new(); // 声音组字典，Key为声音组名称，Value为声音组对象

        private readonly List<int>    m_LoadingSoundList    = new(); // 记录正在加载的声音ID列表
        private readonly HashSet<int> m_LoadingToReleaseSet = new(); // 记录在加载中但是需要释放的声音id集合，防止在加载声音过程中被停止播放的情况

        private EventComponent m_EventComponent; // 事件组件

        private int m_Serial;       // 声音自增序列号(如果播放时指定，则使用指定的序列号，否则自动+1分配)
        
        private AudioMixer m_AudioMixer;       // 混音器
        private AudioListener m_AudioListener; // 声音监听器

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => m_SoundGroupDict.Count;

        /// <summary>
        /// 获取声音混响器。
        /// </summary>
        public AudioMixer AudioMixer => m_AudioMixer;

        /// <summary>
        /// 初始化声音管理器的新实例。
        /// </summary>
        protected override void Init()
        {
            m_Serial       = 0;

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (!m_EventComponent)
            {
                Log.Fatal("[SoundManager] 事件组件不存在!");
                return;
            }

            // 添加AudioListener组件
            m_AudioListener = gameObject.GetOrAddComponent<AudioListener>();

            // 获取声音模块配置数据
            var soundSetting = ModuleSetting.Runtime.ModuleSetting.Instance.SoundSetting;

            // 设置混音器
            m_AudioMixer ??= soundSetting.AudioMixer;

            // 添加声音组
            foreach (var group in soundSetting.AllGroups)
            {
                if (AddSoundGroup(group)) continue;
                Log.Warning("[SoundManager] 添加声音组 '{0}' 失败!", group.Name);
            }

            // 监听场景加载和卸载事件
            SceneManager.sceneLoaded   += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected override void Dispose()
        {
            Shutdown();
            SceneManager.sceneLoaded   -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// 关闭并清理声音管理器。
        /// </summary>
        private void Shutdown()
        {
            StopAllLoadedSounds();
            m_SoundGroupDict.Clear();
            m_LoadingSoundList.Clear();
            m_LoadingToReleaseSet.Clear();
        }

        #region 声音组

        /// <summary>
        /// 是否存在指定声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <returns>指定声音组是否存在。</returns>
        public bool HasSoundGroup(string groupName)
        {
            FuGuard.NotNullOrEmpty(groupName, "[SoundManager]声音组名称");
            return m_SoundGroupDict.ContainsKey(groupName);
        }

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public SoundGroup GetSoundGroup(string groupName)
        {
            FuGuard.NotNullOrEmpty(groupName, "[SoundManager]声音组名称");
            return m_SoundGroupDict.GetValueOrDefault(groupName);
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <returns>所有声音组。</returns>
        public SoundGroup[] GetAllSoundGroups()
        {
            var index   = 0;
            var results = new SoundGroup[m_SoundGroupDict.Count];
            foreach (var (_, soundGroup) in m_SoundGroupDict)
            {
                results[index++] = soundGroup;
            }

            return results;
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <param name="results">所有声音组。</param>
        public void GetAllSoundGroups(List<SoundGroup> results)
        {
            FuGuard.NotNull(results, nameof(results));
            results.Clear();
            foreach (var (_, soundGroup) in m_SoundGroupDict)
            {
                results.Add(soundGroup);
            }
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupInfo">声音组信息。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(SoundGroupInfo soundGroupInfo)
        {
            FuGuard.NotNull(soundGroupInfo, nameof(soundGroupInfo));
            if (HasSoundGroup(soundGroupInfo.Name))
            {
                Log.Info($"[SoundManager]声音组 '{soundGroupInfo.Name}' 已存在，不可重复添加!");
                return false;
            }

            var soundGroupGo = new GameObject($"Sound Group - {soundGroupInfo.Name}");
            soundGroupGo.transform.SetParent(transform);
            soundGroupGo.transform.localScale = Vector3.one;
            var soundGroup = soundGroupGo.GetOrAddComponent<SoundGroup>();
            soundGroup.Init(soundGroupInfo, this);
            m_SoundGroupDict.Add(soundGroupInfo.Name, soundGroup);
            return true;
        }

        #endregion

        #region 声音Get方法

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <returns>所有正在加载声音的序列编号。</returns>
        public int[] GetAllLoadingSoundSerialIds() => m_LoadingSoundList.ToArray();

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载声音的序列编号。</param>
        public void GetAllLoadingSoundSerialIds(List<int> results)
        {
            FuGuard.NotNull(results, nameof(results));
            results.Clear();
            results.AddRange(m_LoadingSoundList);
        }

        /// <summary>
        /// 是否正在加载声音。
        /// </summary>
        /// <param name="serialId">声音序列编号。</param>
        /// <returns>是否正在加载声音。</returns>
        public bool IsLoadingSound(int serialId) => m_LoadingSoundList.Contains(serialId);

        #endregion

        #region 播放声音

        /// <summary>
        /// 播放声音(在指定3D位置播放)
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <param name="extension">声音资源扩展名。</param>
        /// <param name="serialId">序列编号(如果不传入使用默认时，会自动自增后分配一个序列Id)</param>
        /// <param name="soundParams">播放时的声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="onPlayEnd">播放结束回调。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound3DPos(string soundAssetName, string groupName, Vector3 worldPosition, string extension = ".mp3", int serialId = -1, 
            SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)
        {
            var soundParams3D = SoundParams3D.Create(null, worldPosition);
            return PlaySound(soundAssetName, groupName, extension,serialId, soundParams, soundParams3D, userData, onPlayEnd);
        }

        /// <summary>
        /// 播放声音(绑定一个实体)
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <param name="soundParams">播放时的声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="extension">声音资源扩展名。</param>
        /// <param name="serialId">序列编号(如果不传入使用默认时，会自动自增后分配一个序列Id)</param>
        /// <param name="onPlayEnd">播放结束回调。</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySoundToEntity(string soundAssetName, string groupName, Entity.Runtime.Entity bindingEntity, string extension = ".mp3", int serialId = -1, 
            SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)
        {
            var soundParams3D = SoundParams3D.Create(bindingEntity, Vector3.zero);
            return await PlaySound(soundAssetName, groupName, extension, serialId, soundParams, soundParams3D, userData, onPlayEnd);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="extension">声音资源扩展名。</param>
        /// <param name="soundParams">播放时的声音参数。</param>
        /// <param name="soundParams3D"></param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="serialId">序列编号(如果不传入使用默认时，会自动自增后分配一个序列Id)</param>
        /// <param name="onPlayEnd">播放结束回调。</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string groupName, string extension = ".mp3", int serialId = -1, SoundParams soundParams = null,
                                            SoundParams3D soundParams3D = null, object userData = null, Action onPlayEnd = null)
        {
            var  soundAssetPath = Utility.Asset.Path.GetSoundPath(soundAssetName, extension);
            soundParams ??= SoundParams.Create();

            int newSerialId;
            if (serialId >= 0)
                newSerialId = serialId;
            else
                newSerialId = ++m_Serial;

            string               errorMessage = null;
            EPlaySoundErrorCode? errorCode    = null;

            // 检查声音组是否存在
            var soundGroup = GetSoundGroup(groupName);
            if (!soundGroup)
            {
                errorCode    = EPlaySoundErrorCode.SoundGroupNotExist;
                errorMessage = Utility.Text.Format("[SoundManager] 播放声音 '{0}' 失败, 声音组 '{1}' 不存在!", soundAssetPath, groupName);
            }
            else if (soundGroup.SoundAgentCount <= 0)
            {
                errorCode    = EPlaySoundErrorCode.SoundGroupHasNoAgent;
                errorMessage = Utility.Text.Format("[SoundManager]  播放声音 '{0}' 失败, 声音组 '{1}' 没有声音播放代理!", soundAssetPath, groupName);
            }

            if (errorCode.HasValue)
            {
                Log.Error(errorMessage);
                var failureEventArgs = PlaySoundFailureEventArgs.Create(newSerialId, soundAssetPath, groupName, errorCode.Value);
                m_EventComponent.Fire(this, failureEventArgs);
                return newSerialId;
            }

            m_LoadingSoundList.Add(newSerialId);

            // 加载声音资源
            var assetOperationHandle = await AssetManager.Instance.LoadAssetAsync<AudioClip>(soundAssetPath);
            assetOperationHandle.Completed += OnAssetOperationHandleOnCompleted;
            return newSerialId;

            // 加载声音资源完成回调
            void OnAssetOperationHandleOnCompleted(AssetHandle assetHandle)
            {
                var assetObject   = assetHandle.GetAssetObject<AudioClip>();
                var playSoundInfo = PlaySoundInfo.Create(newSerialId, soundAssetPath, assetObject, soundGroup, soundParams, soundParams3D, userData, onPlayEnd);
                LoadAssetSuccessCallback(playSoundInfo);
            }
        }

        #endregion

        #region 停止播放声音

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId) => StopSound(serialId, 0);

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId, float fadeOutSeconds)
        {
            if (IsLoadingSound(serialId))
            {
                m_LoadingToReleaseSet.Add(serialId);
                m_LoadingSoundList.Remove(serialId);
                return true;
            }

            foreach (var (_, soundGroup) in m_SoundGroupDict)
            {
                if (soundGroup.StopSound(serialId, fadeOutSeconds))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 停止所有已加载的声音。
        /// </summary>
        public void StopAllLoadedSounds() => StopAllLoadedSounds(0);

        /// <summary>
        /// 停止所有已加载的声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public void StopAllLoadedSounds(float fadeOutSeconds)
        {
            foreach (var (_, soundGroup) in m_SoundGroupDict)
            {
                soundGroup.StopAllLoadedSounds(fadeOutSeconds);
            }
        }

        /// <summary>
        /// 停止所有正在加载的声音。
        /// </summary>
        public void StopAllLoadingSounds()
        {
            foreach (var serialId in m_LoadingSoundList)
            {
                m_LoadingToReleaseSet.Add(serialId);
            }
        }

        #endregion

        #region 暂停/恢复播放声音

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="serialId">要暂停播放声音的序列编号。</param>
        public void PauseSound(int serialId) => PauseSound(serialId, 0);

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="serialId">要暂停播放声音的序列编号。</param>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public void PauseSound(int serialId, float fadeOutSeconds)
        {
            foreach (var (_, soundGroup) in m_SoundGroupDict)
            {
                if (soundGroup.PauseSound(serialId, fadeOutSeconds)) return;
            }

            throw new FuException(Utility.Text.Format("[SoundManager]找不到声音 '{0}'.", serialId));
        }

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="serialId">要恢复播放声音的序列编号。</param>
        public void ResumeSound(int serialId) => ResumeSound(serialId, 0);

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="serialId">要恢复播放声音的序列编号。</param>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public void ResumeSound(int serialId, float fadeInSeconds)
        {
            foreach (var (_, soundGroup) in m_SoundGroupDict)
            {
                if (soundGroup.ResumeSound(serialId, fadeInSeconds)) return;
            }

            throw new FuException(Utility.Text.Format("[SoundManager]找不到声音 '{0}'.", serialId));
        }

        #endregion

        /// <summary>
        /// 加载声音资源成功回调。
        /// </summary>
        /// <param name="playSoundInfo">播放时的声音信息。</param>
        /// <exception cref="FuException"></exception>
        private void LoadAssetSuccessCallback(PlaySoundInfo playSoundInfo)
        {
            if (playSoundInfo is null)
                throw new FuException("[SoundManager]要播放的声音信息为空!");

            // 如果正在加载但是又被标记为要释放的声音，则播放参数信息对象和释放资源后直接返回
            if (m_LoadingToReleaseSet.Contains(playSoundInfo.SerialId))
            {
                m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
                if (playSoundInfo.SoundParams != null)
                    ReferencePool.Release(playSoundInfo.SoundParams);

                if (playSoundInfo.SoundParams3D != null)
                    ReferencePool.Release(playSoundInfo.SoundParams3D);

                AssetManager.Instance.UnloadAsset(playSoundInfo.SoundAssetPath);
                ReferencePool.Release(playSoundInfo);
                return;
            }

            m_LoadingSoundList.Remove(playSoundInfo.SerialId);

            // 使用声音播放代理播放声音
            var soundAgent = playSoundInfo.SoundGroup.PlaySound(playSoundInfo, out var errorCode);

            // 播放声音成功--派发成功事件, 释放播放参数信息对象
            if (soundAgent)
            {
                Log.Info(Utility.Text.Format("[SoundManager]播放声音 '{0}' 成功, 声音组 '{1}'", playSoundInfo.SoundAssetPath, playSoundInfo.SoundGroup.Name));
                if (playSoundInfo.SoundParams3D != null)
                {
                    // 播放3D声音设置，如果绑定了实体，则设置的绑定实体，否则设置世界坐标
                    if (playSoundInfo.SoundParams3D.BindingEntity)
                        soundAgent.SetBindingEntity(playSoundInfo.SoundParams3D.BindingEntity);
                    else
                        soundAgent.SetWorldPosition(playSoundInfo.SoundParams3D.WorldPosition);
                }

                var successEventArgs = PlaySoundSuccessEventArgs.Create(playSoundInfo.SerialId, playSoundInfo.SoundAssetPath, playSoundInfo.UserData);
                m_EventComponent.Fire(this, successEventArgs);

                if (playSoundInfo.SoundParams != null)
                    ReferencePool.Release(playSoundInfo.SoundParams);

                if (playSoundInfo.SoundParams3D != null)
                    ReferencePool.Release(playSoundInfo.SoundParams3D);

                ReferencePool.Release(playSoundInfo);
                return;
            }

            // 播放声音失败--释放声音资源
            m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
            AssetManager.Instance.UnloadAsset(playSoundInfo.SoundAssetPath);

            var errorCodeValue = EPlaySoundErrorCode.Unknown;
            if (errorCode != null)
                errorCodeValue = errorCode.Value;

            var errorMessage = Utility.Text.Format("[SoundManager]播放声音 '{0}' 失败, 声音组 '{1}', 错误类型 '{2}'.", playSoundInfo.SoundAssetPath, playSoundInfo.SoundGroup.Name, errorCodeValue);
            if (errorCodeValue == EPlaySoundErrorCode.IgnoredBecauseLowPriority)
            {
                Log.Info(errorMessage);
                return;
            }

            Log.Error(errorMessage);

            // 派发播放失败事件
            var failureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, playSoundInfo.SoundAssetPath, playSoundInfo.SoundGroup.Name, errorCodeValue);
            m_EventComponent.Fire(this, failureEventArgs);

            // 释放播放相关信息，并抛出异常
            if (playSoundInfo.SoundParams != null)
                ReferencePool.Release(playSoundInfo.SoundParams);

            if (playSoundInfo.SoundParams3D != null)
                ReferencePool.Release(playSoundInfo.SoundParams3D);

            ReferencePool.Release(playSoundInfo);
            throw new FuException(errorMessage);
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
    }
}