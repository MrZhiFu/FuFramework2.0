using System;
using YooAsset;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Config;
using Hotfix.Game.Config.Tables;
using SoundGroupCfg = Hotfix.Game.Config.Tables.SoundGroup;
using AOT.Framework.Core.Utility;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Framework.Asset;
using System.Collections.Generic;
using Hotfix.Framework.Event;
using Utility = Hotfix.Framework.Core.Utility;

namespace Hotfix.Framework.Sound
{
    /// <summary>
    /// 声音管理模块。
    /// 功能：
    ///     1. 配合资源管理模块，管理声音资源的加载、卸载。
    ///     2. 提供声音播放、暂停、继续、停止等接口。
    ///     3. 提供声音组管理接口。
    /// </summary>
    public sealed partial class SoundModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static SoundModule Instance { get; private set; }

        /// <summary>
        /// 声音组字典，Key为声音组名称，Value为声音组对象
        /// </summary>
        private readonly Dictionary<string, SoundGroup> m_SoundGroupDict = new();

        /// <summary>
        /// 记录正在加载的声音ID列表
        /// </summary>
        private readonly List<int> m_LoadingSoundList = new();

        /// <summary>
        /// 记录在加载中但是需要释放的声音id集合，防止在加载声音过程中被停止播放的情况
        /// </summary>
        private readonly HashSet<int> m_LoadingToReleaseSet = new();

        /// <summary>
        /// 资源管理模块
        /// </summary>
        private AssetModule m_AssetModule;

        /// <summary>
        /// 事件管理模块
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// 声音自增序列号(如果播放时指定，则使用指定的序列号，否则自动+1分配)
        /// </summary>
        private int m_Serial;

        /// <summary>
        /// 混音器
        /// </summary>
        private AudioMixer m_AudioMixer;

        /// <summary>
        /// AudioMixer 资源路径，需在 Unity Editor 中确认实际路径后填入
        /// </summary>
        private const string AudioMixerAssetPath = "Assets/Bundles/Sound/_MainAudioMixer.mixer";

        /// <summary>
        /// 声音监听器
        /// </summary>
        private AudioListener m_AudioListener;

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => m_SoundGroupDict.Count;

        /// <summary>
        /// 获取声音混响器。
        /// </summary>
        public AudioMixer AudioMixer => m_AudioMixer;

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;

            m_Serial = 0;

            m_AssetModule = ModuleManager.GetModule<AssetModule>();
            if (m_AssetModule == null)
            {
                FuLogger.LogFatal("[SoundModule] 资源管理模块不存在!");
                return;
            }

            m_EventModule = ModuleManager.GetModule<EventModule>();
            if (m_EventModule == null)
            {
                FuLogger.LogFatal("[SoundModule] 事件组件不存在!");
                return;
            }

            // 添加AudioListener组件
            var audioListener = new GameObject($"SoundListener");
            m_AudioListener = audioListener.GetOrAddComponent<AudioListener>();

            // 获取声音组配置表
            var tbSoundGroup = ConfigModule.Instance.GetConfig<TbSoundGroup>();
            if (tbSoundGroup == null || tbSoundGroup.Count == 0)
            {
                FuLogger.LogFatal("[SoundModule] 声音组配置表未加载，SoundModule 初始化失败!");
                return;
            }

            // 加载混音器
            LoadAudioMixerAsync().Forget();

            // 添加声音组
            foreach (var row in tbSoundGroup.All)
            {
                if (AddSoundGroup(row)) continue;
                FuLogger.LogWarning($"[SoundModule] 添加声音组 '{row.Id}' 失败!");
            }

            // 监听场景加载和卸载事件
            SceneManager.sceneLoaded   += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            StopAllLoadedSounds();
            m_SoundGroupDict.Clear();
            m_LoadingSoundList.Clear();
            m_LoadingToReleaseSet.Clear();

            SceneManager.sceneLoaded   -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            Instance = null;
        }

        #region 声音组

        /// <summary>
        /// 是否存在指定声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <returns>指定声音组是否存在。</returns>
        public bool HasSoundGroup(string groupName)
        {
            groupName.NotNullOrEmpty("[SoundModule]声音组名称");
            return m_SoundGroupDict.ContainsKey(groupName);
        }

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="groupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public SoundGroup GetSoundGroup(string groupName)
        {
            groupName.NotNullOrEmpty("[SoundModule]声音组名称");
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
            results.NotNull(nameof(results));
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
        public bool AddSoundGroup(SoundGroupCfg row)
        {
            row.NotNull(nameof(row));
            var groupName = row.Id.ToString();
            if (HasSoundGroup(groupName))
            {
                FuLogger.LogInfo($"[SoundModule]声音组 '{groupName}' 已存在，不可重复添加!");
                return false;
            }

            var soundGroupGo = new GameObject($"Sound Group - {groupName}");
            soundGroupGo.transform.localScale = Vector3.one;
            var soundGroup = soundGroupGo.GetOrAddComponent<SoundGroup>();
            soundGroup.Init(row);
            m_SoundGroupDict.Add(groupName, soundGroup);
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
            results.NotNull(nameof(results));
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
            return PlaySound(soundAssetName, groupName, extension, serialId, soundParams, soundParams3D, userData, onPlayEnd);
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
        public async UniTask<int> PlaySoundToEntity(string soundAssetName, string groupName, Entity.Entity bindingEntity, string extension = ".mp3", int serialId = -1,
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
            var soundAssetPath = UtilityAOT.AssetPath.GetSoundPath(soundAssetName, extension);
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
                errorMessage = $"[SoundModule] 播放声音 '{soundAssetPath}' 失败, 声音组 '{groupName}' 不存在!";
            }
            else if (soundGroup.SoundAgentCount <= 0)
            {
                errorCode    = EPlaySoundErrorCode.SoundGroupHasNoAgent;
                errorMessage = $"[SoundModule]  播放声音 '{soundAssetPath}' 失败, 声音组 '{groupName}' 没有声音播放代理!";
            }

            if (errorCode.HasValue)
            {
                FuLogger.LogError(errorMessage);
                var failureEventArgs = PlaySoundFailureEventArgs.Create(newSerialId, soundAssetPath, groupName, errorCode.Value);
                m_EventModule.Broadcast(this, failureEventArgs);
                return newSerialId;
            }

            m_LoadingSoundList.Add(newSerialId);

            // 加载声音资源（await 已保证句柄完成，直接同步处理，避免 Completed 闭包分配）
            try
            {
                var assetOperationHandle = await m_AssetModule.LoadAssetAsync<AudioClip>(soundAssetPath);
                var assetObject          = assetOperationHandle.GetAssetObject<AudioClip>();
                // 句柄随 PlaySoundInfo 流转到 SoundAgent，播放结束时由 SoundAgent.Reset 释放；
                // 中途被丢弃/播放失败时由 LoadAssetSuccessCallback 或 SoundGroup.PlaySound 释放
                var playSoundInfo = PlaySoundInfo.Create(newSerialId, soundAssetPath, assetObject, assetOperationHandle, soundGroup, soundParams, soundParams3D, userData, onPlayEnd);
                LoadAssetSuccessCallback(playSoundInfo);
                return newSerialId;
            }
            catch
            {
                // LoadAssetAsync 抛异常（包未就绪等）：清理 loading/待释放状态，允许重试
                m_LoadingSoundList.Remove(newSerialId);
                m_LoadingToReleaseSet.Remove(newSerialId);
                throw;
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

            throw new InvalidOperationException($"[SoundModule]找不到声音 '{serialId}'.");
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

            throw new InvalidOperationException($"[SoundModule]找不到声音 '{serialId}'.");
        }

        #endregion

        /// <summary>
        /// 加载声音资源成功回调。
        /// </summary>
        /// <param name="playSoundInfo">播放时的声音信息。</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void LoadAssetSuccessCallback(PlaySoundInfo playSoundInfo)
        {
            if (playSoundInfo is null)
                throw new InvalidOperationException("[SoundModule]要播放的声音信息为空!");

            // 如果正在加载但是又被标记为要释放的声音，则释放资源后和释放播放参数信息对象直接返回
            if (m_LoadingToReleaseSet.Contains(playSoundInfo.SerialId))
            {
                m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
                if (playSoundInfo.SoundParams != null)
                    GlobalModule.ReferencePoolModule.Recycle(playSoundInfo.SoundParams);

                if (playSoundInfo.SoundParams3D != null)
                    GlobalModule.ReferencePoolModule.Recycle(playSoundInfo.SoundParams3D);

                playSoundInfo.SoundAssetHandle?.Release(); // 加载中被丢弃，句柄未上代理，释放之
                m_AssetModule.UnloadAsset(playSoundInfo.SoundAssetPath);
                GlobalModule.ReferencePoolModule.Recycle(playSoundInfo);
                return;
            }

            m_LoadingSoundList.Remove(playSoundInfo.SerialId);

            // 使用声音播放代理播放声音
            var soundAgent = playSoundInfo.SoundGroup.PlaySound(playSoundInfo, out var errorCode);

            // 播放声音成功--派发成功事件, 释放播放参数信息对象
            if (soundAgent)
            {
                FuLogger.LogInfo($"[SoundModule]播放声音 '{playSoundInfo.SoundAssetPath}' 成功, 声音组 '{playSoundInfo.SoundGroup.Name}'");
                if (playSoundInfo.SoundParams3D != null)
                {
                    // 播放3D声音设置，如果绑定了实体，则设置的绑定实体，否则设置世界坐标
                    if (playSoundInfo.SoundParams3D.BindingEntity)
                        soundAgent.SetBindingEntity(playSoundInfo.SoundParams3D.BindingEntity);
                    else
                        soundAgent.SetWorldPosition(playSoundInfo.SoundParams3D.WorldPosition);
                }

                var successEventArgs = PlaySoundSuccessEventArgs.Create(playSoundInfo.SerialId, playSoundInfo.SoundAssetPath, playSoundInfo.UserData);
                m_EventModule.Broadcast(this, successEventArgs);

                if (playSoundInfo.SoundParams != null)
                    GlobalModule.ReferencePoolModule.Recycle(playSoundInfo.SoundParams);

                if (playSoundInfo.SoundParams3D != null)
                    GlobalModule.ReferencePoolModule.Recycle(playSoundInfo.SoundParams3D);

                GlobalModule.ReferencePoolModule.Recycle(playSoundInfo);
                return;
            }

            // 播放声音失败--释放声音资源
            m_LoadingToReleaseSet.Remove(playSoundInfo.SerialId);
            m_AssetModule.UnloadAsset(playSoundInfo.SoundAssetPath);

            var errorCodeValue = EPlaySoundErrorCode.Unknown;
            if (errorCode != null)
                errorCodeValue = errorCode.Value;

            var errorMessage = $"[SoundModule]播放声音 '{playSoundInfo.SoundAssetPath}' 失败, 声音组 '{playSoundInfo.SoundGroup.Name}', 错误类型 '{errorCodeValue}'.";
            if (errorCodeValue == EPlaySoundErrorCode.IgnoredBecauseLowPriority)
            {
                FuLogger.LogInfo(errorMessage);
                return;
            }

            FuLogger.LogError(errorMessage);

            // 派发播放失败事件
            var failureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, playSoundInfo.SoundAssetPath, playSoundInfo.SoundGroup.Name, errorCodeValue);
            m_EventModule.Broadcast(this, failureEventArgs);

            // 释放播放相关信息，并抛出异常
            if (playSoundInfo.SoundParams != null)
                GlobalModule.ReferencePoolModule.Recycle(playSoundInfo.SoundParams);

            if (playSoundInfo.SoundParams3D != null)
                GlobalModule.ReferencePoolModule.Recycle(playSoundInfo.SoundParams3D);

            GlobalModule.ReferencePoolModule.Recycle(playSoundInfo);
            throw new InvalidOperationException(errorMessage);
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
        private void RefreshAudioListener()
        {
            m_AudioListener.enabled = UnityEngine.Object.FindObjectsOfType<AudioListener>().Length <= 1;
        }

        /// <summary>
        /// 异步加载 AudioMixer 资源。
        /// </summary>
        private async UniTaskVoid LoadAudioMixerAsync()
        {
            try
            {
                var handle = await m_AssetModule.LoadAssetAsync<AudioMixer>(AudioMixerAssetPath);
                if (handle.Status == EOperationStatus.Succeeded)
                    m_AudioMixer = handle.GetAssetObject<AudioMixer>();
                else
                    FuLogger.LogFatal($"[SoundModule] AudioMixer 加载失败: {AudioMixerAssetPath} - {handle.Error}");

                handle.Release(); // 释放句柄，AudioMixer 对象已由 m_AudioMixer 持有
            }
            catch (Exception e)
            {
                FuLogger.LogFatal($"[SoundModule] AudioMixer 加载异常: {AudioMixerAssetPath} - {e.Message}");
            }
        }
    }
}
