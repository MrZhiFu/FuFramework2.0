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

        private Dictionary<string, SoundGroup> m_SoundGroupDict; // 声音组字典，Key为声音组名称，Value为声音组对象
        private List<int> m_LoadingSoundList; // 记录正在加载的声音ID列表
        private HashSet<int> m_LoadingToReleaseSet; // 记录在加载中但是需要释放的声音id集合，防止在加载声音过程中被停止播放的情况

        private IAssetManager m_assetManager; // 资源管理器
        private EventComponent m_EventComponent; // 事件组件

        private int m_Serial; // 声音自增序列号
        private Transform m_InstanceRoot; // Sound实例根节点
        private AudioMixer m_AudioMixer; // 混音器
        private AudioListener m_AudioListener; // 声音监听器

        /// <summary>
        /// 初始化声音管理器的新实例。
        /// </summary>
        protected override void Init()
        {
            m_SoundGroupDict = new Dictionary<string, SoundGroup>(StringComparer.Ordinal);
            m_LoadingSoundList = new List<int>();
            m_LoadingToReleaseSet = new HashSet<int>();

            m_Serial = 0;
            m_assetManager = FuEntry.GetModule<IAssetManager>();

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (!m_EventComponent)
            {
                Log.Fatal("[SoundManager] 事件组件不存在!");
                return;
            }

            if (!m_InstanceRoot)
            {
                m_InstanceRoot = new GameObject("Sound Instances").transform;
                m_InstanceRoot.SetParent(transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            m_AudioListener = gameObject.GetOrAddComponent<AudioListener>();

            // 获取声音配置数据
            var soundSetting = ModuleSetting.Runtime.ModuleSetting.Instance.SoundSetting;

            // 设置混音器
            m_AudioMixer ??= soundSetting.AudioMixer;

            // 添加声音组
            foreach (var group in soundSetting.AllGroups)
            {
                if (AddSoundGroup(group)) continue;
                Log.Warning("[SoundManager] 添加声音组 '{0}' 失败!", group.Name);
            }
        }

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => m_SoundGroupDict.Count;

        /// <summary>
        /// 获取声音混响器。
        /// </summary>
        public AudioMixer AudioMixer => m_AudioMixer;

        protected override void Dispose()
        {
            Shutdown();
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
            if (string.IsNullOrEmpty(groupName)) throw new FuException("[SoundManager]声音组名称为空!");
            return m_SoundGroupDict.ContainsKey(groupName);
        }

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public SoundGroup GetSoundGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) throw new FuException("[SoundManager]声音组名称为空!");
            return m_SoundGroupDict.GetValueOrDefault(groupName);
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <returns>所有声音组。</returns>
        public SoundGroup[] GetAllSoundGroups()
        {
            var index = 0;
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
            if (results == null) throw new FuException("[SoundManager]参数结果列表为空!");
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
            soundGroupGo.transform.SetParent(m_InstanceRoot);
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
            if (results == null) throw new FuException("[SoundManager]参数结果列表为空!");
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
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string groupName)
            => PlaySound(soundAssetName, groupName, Constant.DefaultPriority, null, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string groupName, int priority)
            => PlaySound(soundAssetName, groupName, priority, null, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string groupName, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, groupName, Constant.DefaultPriority, playSoundParams, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string groupName, object userData)
            => PlaySound(soundAssetName, groupName, Constant.DefaultPriority, null, userData);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string groupName, int priority, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, groupName, priority, playSoundParams, null);

        /// <summary>
        /// 播放声音。并设置指定的序列编号
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="serialId">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySoundBySerialId(string soundAssetName, string groupName, int serialId)
            => PlaySound(soundAssetName, groupName, Constant.DefaultPriority, null, null, serialId);

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
        /// <param name="groupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string groupName, int priority, object userData)
            => PlaySound(soundAssetName, groupName, priority, null, userData);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string groupName, PlaySoundParams playSoundParams, object userData)
            => await PlaySound(soundAssetName, groupName, Constant.DefaultPriority, playSoundParams, userData);

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
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams,
            Vector3 worldPosition, object userData)
        {
            var playSoundInfoExtra = PlaySoundInfoExtra.Create(null, worldPosition, userData);
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, playSoundInfoExtra, -1);
        }

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
        public async UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity.Runtime.Entity bindingEntity, object userData, int serialId)
        {
            var playSoundInfoExtra = PlaySoundInfoExtra.Create(bindingEntity, Vector3.zero, userData);
            return await PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, playSoundInfoExtra, serialId);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="serialId">序列编号</param>
        /// <param name="isLoop">是否循环播放。</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string groupName, int priority, PlaySoundParams playSoundParams, object userData, int serialId = -1, bool isLoop = false)
        {
            if (m_assetManager == null) throw new FuException("[SoundManager]声音资源管理器为空!");

            playSoundParams ??= PlaySoundParams.Create(isLoop, priority);

            int newSerialId;
            if (serialId >= 0)
                newSerialId = serialId;
            else
                newSerialId = ++m_Serial;

            EPlaySoundErrorCode? errorCode = null;
            string errorMessage = null;

            // 检查声音组是否存在
            var soundGroup = GetSoundGroup(groupName);
            if (soundGroup == null)
            {
                errorCode = EPlaySoundErrorCode.SoundGroupNotExist;
                errorMessage = Utility.Text.Format("[SoundManager] 播放声音 '{0}' 失败, 声音组 '{1}' 不存在!", soundAssetName, groupName);
            }
            else if (soundGroup.SoundAgentCount <= 0)
            {
                errorCode = EPlaySoundErrorCode.SoundGroupHasNoAgent;
                errorMessage = Utility.Text.Format("[SoundManager]  播放声音 '{0}' 失败, 声音组 '{1}' 没有声音播放代理!", soundAssetName, groupName);
            }

            if (errorCode.HasValue)
            {
                Log.Error(errorMessage);
                var failureEventArgs = PlaySoundFailureEventArgs.Create(newSerialId, soundAssetName, groupName, errorCode.Value, errorMessage, userData);
                m_EventComponent.Fire(this, failureEventArgs);
                ReferencePool.Release(failureEventArgs);
                return newSerialId;
            }

            m_LoadingSoundList.Add(newSerialId);

            // 加载声音资源
            var assetOperationHandle = await m_assetManager.LoadAssetAsync<AudioClip>(soundAssetName);
            assetOperationHandle.Completed += OnAssetOperationHandleOnCompleted;
            return newSerialId;

            // 加载声音资源完成回调
            void OnAssetOperationHandleOnCompleted(AssetHandle assetHandle)
            {
                var assetObject = assetHandle.GetAssetObject<AudioClip>();
                var playSoundInfo = PlaySoundInfo.Create(newSerialId, soundGroup, playSoundParams, userData);
                LoadAssetSuccessCallback(soundAssetName, assetObject, assetHandle.Duration, playSoundInfo);
            }
        }

        #endregion

        #region 停止播放声音

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId) => StopSound(serialId, Constant.DefaultFadeOutSeconds);

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
        public void StopAllLoadedSounds() => StopAllLoadedSounds(Constant.DefaultFadeOutSeconds);

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
        public void PauseSound(int serialId) => PauseSound(serialId, Constant.DefaultFadeOutSeconds);

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
        public void ResumeSound(int serialId) => ResumeSound(serialId, Constant.DefaultFadeInSeconds);

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
        /// <param name="soundAssetName"></param>
        /// <param name="soundAsset"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        /// <exception cref="FuException"></exception>
        private void LoadAssetSuccessCallback(string soundAssetName, object soundAsset, float duration, object userData)
        {
            if (userData is not PlaySoundInfo playSoundInfo) throw new FuException("[SoundManager]要播放的声音信息为空!");

            // 如果正在加载但是又被标记为要释放的声音，则直接释放资源并返回
            if (m_LoadingToReleaseSet.Contains(playSoundInfo.SerialId))
            {
                m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
                if (playSoundInfo.PlaySoundParams.Referenced) ReferencePool.Release(playSoundInfo.PlaySoundParams);
                ReferencePool.Release(playSoundInfo);
                m_assetManager?.UnloadAsset(soundAssetName);
                return;
            }

            m_LoadingSoundList.Remove(playSoundInfo.SerialId);

            // 播放声音成功--派发成功事件
            var soundAgent = playSoundInfo.SoundGroup.PlaySound(playSoundInfo.SerialId, soundAsset, playSoundInfo.PlaySoundParams, out var errorCode);
            if (soundAgent)
            {
                Log.Info(Utility.Text.Format("[SoundManager]播放声音 '{0}' 成功, 声音组 '{1}'", soundAssetName, playSoundInfo.SoundGroup.Name));
                var successEventArgs = PlaySoundSuccessEventArgs.Create(playSoundInfo.SerialId, soundAssetName, soundAgent, duration, playSoundInfo.UserData);
                if (successEventArgs.UserData is PlaySoundInfoExtra playSoundInfoExtra)
                {
                    if (playSoundInfoExtra.BindingEntity)
                        soundAgent.SetBindingEntity(playSoundInfoExtra.BindingEntity);
                    else
                        soundAgent.SetWorldPosition(playSoundInfoExtra.WorldPosition);
                }

                m_EventComponent.Fire(this, successEventArgs);
                ReferencePool.Release(successEventArgs);

                if (playSoundInfo.PlaySoundParams.Referenced) ReferencePool.Release(playSoundInfo.PlaySoundParams);
                ReferencePool.Release(playSoundInfo);
                return;
            }

            // 播放声音失败--释放声音资源，派发失败事件
            m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
            m_assetManager?.UnloadAsset(soundAssetName);
            
            var errorCodeValue = EPlaySoundErrorCode.Unknown;
            if (errorCode != null)
                errorCodeValue = errorCode.Value;

            var errorMessage = Utility.Text.Format("[SoundManager]播放声音 '{0}' 失败, 声音组 '{1}', 错误类型 '{2}'.", soundAssetName, playSoundInfo.SoundGroup.Name, errorCodeValue);
            var failureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, soundAssetName, playSoundInfo.SoundGroup.Name, errorCodeValue, errorMessage, playSoundInfo.UserData);

            if (failureEventArgs.ErrorCode == EPlaySoundErrorCode.IgnoredBecauseLowPriority)
                Log.Info(errorMessage);
            else
                Log.Error(errorMessage);

            m_EventComponent.Fire(this, failureEventArgs);
            ReferencePool.Release(failureEventArgs);

            // 释放播放相关信息，并抛出异常
            if (playSoundInfo.PlaySoundParams.Referenced)
                ReferencePool.Release(playSoundInfo.PlaySoundParams);
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