using System;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
using Utility = FuFramework.Core.Runtime.Utility;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 声音管理器。
    /// 功能：实现了声音管理器相关接口，包括声音组、声音播放，暂停，继续，停止等。
    /// </summary>
    public sealed partial class SoundManager : FuModule, ISoundManager
    {
        private readonly Dictionary<string, SoundGroup> m_SoundGroups;
        private readonly List<int>                      m_SoundsBeingLoaded;

        private readonly HashSet<int> m_SoundsToReleaseOnLoad;

        private IAssetManager                           _assetManager;
        private ISoundHelper                            m_SoundHelper;
        private int                                     m_Serial;
        private EventHandler<PlaySoundSuccessEventArgs> m_PlaySoundSuccessEventHandler;
        private EventHandler<PlaySoundFailureEventArgs> m_PlaySoundFailureEventHandler;

        /// <summary>
        /// 初始化声音管理器的新实例。
        /// </summary>
        public SoundManager()
        {
            m_SoundGroups           = new Dictionary<string, SoundGroup>(StringComparer.Ordinal);
            m_SoundsBeingLoaded     = new List<int>();
            m_SoundsToReleaseOnLoad = new HashSet<int>();

            m_Serial      = 0;
            _assetManager = null;
            m_SoundHelper = null;

            m_PlaySoundSuccessEventHandler = null;
            m_PlaySoundFailureEventHandler = null;
        }

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => m_SoundGroups.Count;

        /// <summary>
        /// 播放声音成功事件。
        /// </summary>
        public event EventHandler<PlaySoundSuccessEventArgs> PlaySoundSuccess
        {
            add => m_PlaySoundSuccessEventHandler += value;
            remove => m_PlaySoundSuccessEventHandler -= value;
        }

        /// <summary>
        /// 播放声音失败事件。
        /// </summary>
        public event EventHandler<PlaySoundFailureEventArgs> PlaySoundFailure
        {
            add => m_PlaySoundFailureEventHandler += value;
            remove => m_PlaySoundFailureEventHandler -= value;
        }

        /// <summary>
        /// 声音管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 关闭并清理声音管理器。
        /// </summary>
        protected override void Shutdown()
        {
            StopAllLoadedSounds();
            m_SoundGroups.Clear();
            m_SoundsBeingLoaded.Clear();
            m_SoundsToReleaseOnLoad.Clear();
        }

        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="assetManager">资源管理器。</param>
        public void SetResourceManager(IAssetManager assetManager)
        {
            _assetManager = assetManager ?? throw new FuException("Resource manager is invalid.");
        }

        /// <summary>
        /// 设置声音辅助器。
        /// </summary>
        /// <param name="soundHelper">声音辅助器。</param>
        public void SetSoundHelper(ISoundHelper soundHelper)
        {
            m_SoundHelper = soundHelper ?? throw new FuException("Sound helper is invalid.");
        }

        /// <summary>
        /// 是否存在指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>指定声音组是否存在。</returns>
        public bool HasSoundGroup(string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName)) throw new FuException("Sound group name is invalid.");
            return m_SoundGroups.ContainsKey(soundGroupName);
        }

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public ISoundGroup GetSoundGroup(string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName)) throw new FuException("Sound group name is invalid.");
            return m_SoundGroups.GetValueOrDefault(soundGroupName);
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <returns>所有声音组。</returns>
        public ISoundGroup[] GetAllSoundGroups()
        {
            var index   = 0;
            var results = new ISoundGroup[m_SoundGroups.Count];
            foreach (var (_, soundGroup) in m_SoundGroups)
            {
                results[index++] = soundGroup;
            }

            return results;
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <param name="results">所有声音组。</param>
        public void GetAllSoundGroups(List<ISoundGroup> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            foreach (var (_, soundGroup) in m_SoundGroups)
            {
                results.Add(soundGroup);
            }
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundGroupHelper">声音组辅助器。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string soundGroupName, ISoundGroupHelper soundGroupHelper)
        {
            return AddSoundGroup(soundGroupName, false, Constant.DefaultMute, Constant.DefaultVolume, soundGroupHelper);
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundGroupAvoidBeingReplacedBySamePriority">声音组中的声音是否避免被同优先级声音替换。</param>
        /// <param name="soundGroupMute">声音组是否静音。</param>
        /// <param name="soundGroupVolume">声音组音量。</param>
        /// <param name="soundGroupHelper">声音组辅助器。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string soundGroupName, bool soundGroupAvoidBeingReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume, ISoundGroupHelper soundGroupHelper)
        {
            if (string.IsNullOrEmpty(soundGroupName)) throw new FuException("Sound group name is invalid.");
            if (soundGroupHelper == null) throw new FuException("Sound group helper is invalid.");
            if (HasSoundGroup(soundGroupName)) return false;

            var soundGroup = new SoundGroup(soundGroupName, soundGroupHelper)
            {
                AvoidBeingReplacedBySamePriority = soundGroupAvoidBeingReplacedBySamePriority,
                Mute                             = soundGroupMute,
                Volume                           = soundGroupVolume
            };

            m_SoundGroups.Add(soundGroupName, soundGroup);
            return true;
        }

        /// <summary>
        /// 增加声音代理辅助器。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundAgentHelper">要增加的声音代理辅助器。</param>
        public void AddSoundAgentHelper(string soundGroupName, ISoundAgentHelper soundAgentHelper)
        {
            if (m_SoundHelper == null) throw new FuException("You must set sound helper first.");
            var soundGroup = (SoundGroup)GetSoundGroup(soundGroupName);
            if (soundGroup == null) throw new FuException(Utility.Text.Format("Sound group '{0}' is not exist.", soundGroupName));
            soundGroup.AddSoundAgentHelper(m_SoundHelper, soundAgentHelper);
        }

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <returns>所有正在加载声音的序列编号。</returns>
        public int[] GetAllLoadingSoundSerialIds() => m_SoundsBeingLoaded.ToArray();

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载声音的序列编号。</param>
        public void GetAllLoadingSoundSerialIds(List<int> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            results.AddRange(m_SoundsBeingLoaded);
        }

        /// <summary>
        /// 是否正在加载声音。
        /// </summary>
        /// <param name="serialId">声音序列编号。</param>
        /// <returns>是否正在加载声音。</returns>
        public bool IsLoadingSound(int serialId) => m_SoundsBeingLoaded.Contains(serialId);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName)
            => PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, null, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority)
            => PlaySound(soundAssetName, soundGroupName, priority, null, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, playSoundParams, null);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, object userData)
            => PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, null, userData);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams)
            => PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null);

        /// <summary>
        /// 播放声音。并设置指定的序列编号
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="serialId">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySoundBySerialId(string soundAssetName, string soundGroupName, int serialId)
            => PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, null, null, serialId);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, object userData)
            => PlaySound(soundAssetName, soundGroupName, priority, null, userData);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams, object userData)
            => await PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, playSoundParams, userData);

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="serialId">序列编号</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, object userData, int serialId = -1)
        {
            if (_assetManager == null) throw new FuException("You must set resource manager first.");
            if (m_SoundHelper == null) throw new FuException("You must set sound helper first.");
            playSoundParams ??= PlaySoundParams.Create();

            int newSerialId;
            if (serialId >= 0)
                newSerialId = serialId;
            else
                newSerialId = ++m_Serial;


            PlaySoundErrorCode? errorCode = null;

            string errorMessage = null;
            var    soundGroup   = (SoundGroup)GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                errorCode    = PlaySoundErrorCode.SoundGroupNotExist;
                errorMessage = Utility.Text.Format("Sound group '{0}' is not exist.", soundGroupName);
            }
            else if (soundGroup.SoundAgentCount <= 0)
            {
                errorCode    = PlaySoundErrorCode.SoundGroupHasNoAgent;
                errorMessage = Utility.Text.Format("Sound group '{0}' is have no sound agent.", soundGroupName);
            }

            if (errorCode.HasValue)
            {
                if (m_PlaySoundFailureEventHandler == null) throw new FuException(errorMessage);
                var playSoundFailureEventArgs = PlaySoundFailureEventArgs.Create(newSerialId, soundAssetName, soundGroupName, playSoundParams, errorCode.Value, errorMessage, userData);
                m_PlaySoundFailureEventHandler(this, playSoundFailureEventArgs);
                ReferencePool.Release(playSoundFailureEventArgs);

                if (playSoundParams.Referenced)
                    ReferencePool.Release(playSoundParams);

                return newSerialId;
            }

            m_SoundsBeingLoaded.Add(newSerialId);
            var assetOperationHandle = await _assetManager.LoadAssetAsync<AudioClip>(soundAssetName);
            assetOperationHandle.Completed += OnAssetOperationHandleOnCompleted;
            return newSerialId;

            // 加载声音资源完成回调
            void OnAssetOperationHandleOnCompleted(AssetHandle assetHandle)
            {
                var assetObject = assetHandle.GetAssetObject<AudioClip>();
                LoadAssetSuccessCallback(soundAssetName, assetObject, assetHandle.Duration, PlaySoundInfo.Create(newSerialId, soundGroup, playSoundParams, userData));
            }
        }

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
                m_SoundsToReleaseOnLoad.Add(serialId);
                m_SoundsBeingLoaded.Remove(serialId);
                return true;
            }

            foreach (var (_, soundGroup) in m_SoundGroups)
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
            foreach (var (_, soundGroup) in m_SoundGroups)
            {
                soundGroup.StopAllLoadedSounds(fadeOutSeconds);
            }
        }

        /// <summary>
        /// 停止所有正在加载的声音。
        /// </summary>
        public void StopAllLoadingSounds()
        {
            foreach (var serialId in m_SoundsBeingLoaded)
            {
                m_SoundsToReleaseOnLoad.Add(serialId);
            }
        }

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
            foreach (var (_, soundGroup) in m_SoundGroups)
            {
                if (soundGroup.PauseSound(serialId, fadeOutSeconds))
                    return;
            }

            throw new FuException(Utility.Text.Format("Can not find sound '{0}'.", serialId));
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
            foreach (var (_, soundGroup) in m_SoundGroups)
            {
                if (soundGroup.ResumeSound(serialId, fadeInSeconds))
                    return;
            }

            throw new FuException(Utility.Text.Format("Can not find sound '{0}'.", serialId));
        }

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
            if (userData is not PlaySoundInfo playSoundInfo) throw new FuException("Play sound info is invalid.");

            if (m_SoundsToReleaseOnLoad.Contains(playSoundInfo.SerialId))
            {
                m_SoundsToReleaseOnLoad.Remove(playSoundInfo.SerialId);
                if (playSoundInfo.PlaySoundParams.Referenced)
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);

                ReferencePool.Release(playSoundInfo);
                m_SoundHelper.ReleaseSoundAsset(soundAsset);
                return;
            }

            m_SoundsBeingLoaded.Remove(playSoundInfo.SerialId);

            // 播放声音
            var soundAgent = playSoundInfo.SoundGroup.PlaySound(playSoundInfo.SerialId, soundAsset, playSoundInfo.PlaySoundParams, out var errorCode);
            if (soundAgent != null)
            {
                if (m_PlaySoundSuccessEventHandler != null)
                {
                    var playSoundSuccessEventArgs = PlaySoundSuccessEventArgs.Create(playSoundInfo.SerialId, soundAssetName, soundAgent, duration, playSoundInfo.UserData);
                    m_PlaySoundSuccessEventHandler(this, playSoundSuccessEventArgs);
                    ReferencePool.Release(playSoundSuccessEventArgs);
                }

                if (playSoundInfo.PlaySoundParams.Referenced)
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);

                ReferencePool.Release(playSoundInfo);
                return;
            }

            m_SoundsToReleaseOnLoad.Remove(playSoundInfo.SerialId);
            m_SoundHelper.ReleaseSoundAsset(soundAsset);

            // 播放声音失败
            var errorMessage = Utility.Text.Format("Sound group '{0}' play sound '{1}' failure.", playSoundInfo.SoundGroup.Name, soundAssetName);
            if (m_PlaySoundFailureEventHandler != null)
            {
                var errorCodeValue = PlaySoundErrorCode.Unknown;
                if (errorCode != null)
                    errorCodeValue = errorCode.Value;

                var playSoundFailureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, soundAssetName, playSoundInfo.SoundGroup.Name,
                                                                                 playSoundInfo.PlaySoundParams, errorCodeValue, errorMessage, playSoundInfo.UserData);
                m_PlaySoundFailureEventHandler(this, playSoundFailureEventArgs);
                ReferencePool.Release(playSoundFailureEventArgs);

                if (playSoundInfo.PlaySoundParams.Referenced)
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);

                ReferencePool.Release(playSoundInfo);
                return;
            }

            if (playSoundInfo.PlaySoundParams.Referenced)
                ReferencePool.Release(playSoundInfo.PlaySoundParams);

            ReferencePool.Release(playSoundInfo);
            throw new FuException(errorMessage);
        }
    }
}