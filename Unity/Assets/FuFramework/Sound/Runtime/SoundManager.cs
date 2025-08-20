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
        private readonly Dictionary<string, SoundGroup> m_SoundGroupDict; // 声音组字典，Key为声音组名称，Value为声音组对象
        private readonly List<int> m_LoadingSoundList; // 记录正在加载的声音ID列表
        private readonly HashSet<int> m_LoadingToReleaseSet; // 记录在加载中但是需要释放的声音id集合，防止在加载声音过程中被停止播放的情况

        private int m_Serial; // 声音自增序列号
        private IAssetManager   m_assetManager; // 资源管理器

        private EventHandler<PlaySoundSuccessEventArgs> m_PlaySoundSuccessEventHandler; // 播放声音成功事件
        private EventHandler<PlaySoundFailureEventArgs> m_PlaySoundFailureEventHandler; // 播放声音失败事件

        /// <summary>
        /// 初始化声音管理器的新实例。
        /// </summary>
        public SoundManager()
        {
            m_SoundGroupDict = new Dictionary<string, SoundGroup>(StringComparer.Ordinal);
            m_LoadingSoundList = new List<int>();
            m_LoadingToReleaseSet = new HashSet<int>();

            m_Serial = 0;
            m_assetManager = null;

            m_PlaySoundSuccessEventHandler = null;
            m_PlaySoundFailureEventHandler = null;
        }

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => m_SoundGroupDict.Count;

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
            m_SoundGroupDict.Clear();
            m_LoadingSoundList.Clear();
            m_LoadingToReleaseSet.Clear();
        }

        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="assetManager">资源管理器。</param>
        public void SetResourceManager(IAssetManager assetManager)
        {
            m_assetManager = assetManager ?? throw new FuException("[SoundManager]声音资源管理器为空!");
        }

        #region 声音组

        /// <summary>
        /// 是否存在指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>指定声音组是否存在。</returns>
        public bool HasSoundGroup(string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName)) throw new FuException("[SoundManager]声音组名称为空!");
            return m_SoundGroupDict.ContainsKey(soundGroupName);
        }

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public SoundGroup GetSoundGroup(string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName)) throw new FuException("[SoundManager]声音组名称为空!");
            return m_SoundGroupDict.GetValueOrDefault(soundGroupName);
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
        /// <param name="groupName">声音组名称。</param>
        /// <param name="soundGroupHelper">声音组辅助器。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string groupName, ISoundGroupHelper soundGroupHelper)
        {
            return AddSoundGroup(groupName, false, Constant.DefaultMute, Constant.DefaultVolume, soundGroupHelper);
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <param name="allowBeReplacedBySamePriority">声音组中的声音是否允许被同优先级声音替换。</param>
        /// <param name="soundGroupMute">声音组是否静音。</param>
        /// <param name="soundGroupVolume">声音组音量。</param>
        /// <param name="groupHelper">声音组辅助器。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string groupName, bool allowBeReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume,
            ISoundGroupHelper groupHelper)
        {
            if (string.IsNullOrEmpty(groupName)) throw new FuException("[SoundManager]声音组名称为空!");
            if (groupHelper == null) throw new FuException("[SoundManager]声音组辅助器为空!");
            if (HasSoundGroup(groupName)) return false;

            var soundGroup = new SoundGroup(groupName, groupHelper)
            {
                AllowBeReplacedBySamePriority = allowBeReplacedBySamePriority,
                Mute = soundGroupMute,
                Volume = soundGroupVolume
            };

            m_SoundGroupDict.Add(groupName, soundGroup);
            return true;
        }

        /// <summary>
        /// 增加声音代理辅助器。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundAgentHelper">要增加的声音代理辅助器。</param>
        public void AddSoundAgentHelper(string soundGroupName, ISoundAgentHelper soundAgentHelper)
        {
            var soundGroup = GetSoundGroup(soundGroupName);
            if (soundGroup == null) throw new FuException(Utility.Text.Format("[SoundManager]声音组 '{0}' 不存在!.", soundGroupName));
            soundGroup.AddSoundAgentHelper(soundAgentHelper, this);
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
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="serialId">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public UniTask<int> PlaySoundBySerialId(string soundAssetName, string soundGroupName, int serialId)
            => PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, null, null, serialId);

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
        /// <param name="groupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="serialId">序列编号</param>
        /// <returns>声音的序列编号。</returns>
        public async UniTask<int> PlaySound(string soundAssetName, string groupName, int priority, PlaySoundParams playSoundParams,
            object userData, int serialId = -1)
        {
            if (m_assetManager == null) throw new FuException("[SoundManager]声音资源管理器为空!");
            
            playSoundParams ??= PlaySoundParams.Create();

            int newSerialId;
            if (serialId >= 0)
                newSerialId = serialId;
            else
                newSerialId = ++m_Serial;
            
            PlaySoundErrorCode? errorCode = null;
            string errorMessage = null;
            
            // 检查声音组是否存在
            var soundGroup = GetSoundGroup(groupName);
            if (soundGroup == null)
            {
                errorCode = PlaySoundErrorCode.SoundGroupNotExist;
                errorMessage = Utility.Text.Format("[SoundManager] 声音组 '{0}' 不存在!", groupName);
            }
            else if (soundGroup.SoundAgentCount <= 0)
            {
                errorCode = PlaySoundErrorCode.SoundGroupHasNoAgent;
                errorMessage = Utility.Text.Format("[SoundManager] 声音组 '{0}' 没有声音播放代理!", groupName);
            }

            if (errorCode.HasValue)
            {
                // 播放声音失败
                if (m_PlaySoundFailureEventHandler == null) throw new FuException(errorMessage);
                
                var playSoundFailureEventArgs = PlaySoundFailureEventArgs.Create(newSerialId, soundAssetName, groupName, playSoundParams,
                    errorCode.Value, errorMessage, userData);
                
                m_PlaySoundFailureEventHandler(this, playSoundFailureEventArgs);
                ReferencePool.Release(playSoundFailureEventArgs);

                if (playSoundParams.Referenced)
                    ReferencePool.Release(playSoundParams);

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

            // 播放声音成功
            var soundAgent = playSoundInfo.SoundGroup.PlaySound(playSoundInfo.SerialId, soundAsset, playSoundInfo.PlaySoundParams, out var errorCode);
            if (soundAgent != null)
            {
                if (m_PlaySoundSuccessEventHandler != null)
                {
                    var successEventArgs = PlaySoundSuccessEventArgs.Create(playSoundInfo.SerialId, soundAssetName, soundAgent, duration, playSoundInfo.UserData);
                    m_PlaySoundSuccessEventHandler(this, successEventArgs);
                    ReferencePool.Release(successEventArgs);
                }

                if (playSoundInfo.PlaySoundParams.Referenced) ReferencePool.Release(playSoundInfo.PlaySoundParams);
                ReferencePool.Release(playSoundInfo);
                return;
            }

            m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
            
            // 释放声音资源
            m_assetManager?.UnloadAsset(soundAssetName);

            // 播放声音失败
            var errorMessage = Utility.Text.Format("[SoundManager]声音组 '{0}' 播放声音 '{1}' 失败!.", playSoundInfo.SoundGroup.Name, soundAssetName);
            if (m_PlaySoundFailureEventHandler != null)
            {
                var errorCodeValue = PlaySoundErrorCode.Unknown;
                if (errorCode != null) errorCodeValue = errorCode.Value;

                var failureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, soundAssetName, 
                    playSoundInfo.SoundGroup.Name, playSoundInfo.PlaySoundParams, errorCodeValue, errorMessage, playSoundInfo.UserData);
                
                m_PlaySoundFailureEventHandler(this, failureEventArgs);
                ReferencePool.Release(failureEventArgs);
            }

            // 释放播放相关信息，并抛出异常
            if (playSoundInfo.PlaySoundParams.Referenced) ReferencePool.Release(playSoundInfo.PlaySoundParams);
            ReferencePool.Release(playSoundInfo);
            throw new FuException(errorMessage);
        }
    }
}