using System.Collections.Generic;
using System.Linq;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;
using UnityEngine.Audio;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    public partial class SoundManager
    {
        /// <summary>
        /// 声音组。
        /// 功能:管理该组中的声音, 包括播放、停止、暂停、恢复等。
        /// </summary>
        public class SoundGroup : MonoBehaviour
        {
            /// <summary>
            /// 声音播放代理列表
            /// </summary>
            private readonly List<SoundAgent> m_SoundAgents = new();

            /// <summary>
            /// 是否静音
            /// </summary>
            private bool m_Mute;

            /// <summary>
            /// 声音组音量。
            /// </summary>
            private float m_Volume;

            /// <summary>
            /// 获取声音组名称。
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// 获取或设置声音组辅助器所在的混音组。
            /// </summary>
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public AudioMixerGroup AudioMixerGroup { get; private set; }

            /// <summary>
            /// 获取或设置声音组中的声音是否允许被同优先级声音替换。
            /// </summary>
            public bool AllowBeReplacedBySamePriority { get; private set; }

            /// <summary>
            /// 获取声音代理数。
            /// </summary>
            public int SoundAgentCount => m_SoundAgents.Count;

            /// <summary>
            /// 获取或设置声音组静音。
            /// </summary>
            public bool Mute
            {
                get => m_Mute;
                set
                {
                    if (value == m_Mute) return;
                    m_Mute = value;
                    // TODO：这里需要保存声音组的设置到本地，以便下次打开游戏时还原。
                    foreach (var soundAgent in m_SoundAgents)
                    {
                        soundAgent.RefreshMute();
                    }
                }
            }

            /// <summary>
            /// 获取或设置声音组音量。
            /// </summary>
            public float Volume
            {
                get => m_Volume;
                set
                {
                    if (Mathf.Approximately(value, m_Volume)) return;
                    m_Volume = value;
                    // TODO：这里需要保存声音组的设置到本地，以便下次打开游戏时还原。
                    foreach (var soundAgent in m_SoundAgents)
                    {
                        soundAgent.RefreshVolume();
                    }
                }
            }

            /// <summary>
            /// 初始化声音组的新实例。
            /// </summary>
            /// <param name="soundGroupInfo">声音组信息。</param>
            /// <param name="soundManager">声音管理器。</param>
            public void Init(SoundGroupInfo soundGroupInfo, SoundManager soundManager)
            {
                FuGuard.NotNull(soundGroupInfo, nameof(soundGroupInfo));
                Name                          = soundGroupInfo.Name;
                AllowBeReplacedBySamePriority = soundGroupInfo.AllowBeReplacedBySamePriority;

                // TODO：这里获取玩家是否存储了相关的设置，如果是，则使用玩家的设置，否则使用默认设置。
                Volume = soundGroupInfo.Volume;
                Mute   = soundGroupInfo.Mute;

                // 添加声音组辅助器中的声音播放代理辅助器
                for (var i = 0; i < soundGroupInfo.AgentCount; i++)
                {
                    AddSoundAgentHelper(i, soundManager);
                }
            }

            /// <summary>
            /// 增加声音代理辅助器。
            /// </summary>
            /// <param name="idx">声音代理索引。</param>
            /// <param name="manager">声音管理器。</param>
            public void AddSoundAgentHelper(int idx, SoundManager manager)
            {
                var soundAgentGo = new GameObject($"Sound Agent - {idx}");
                soundAgentGo.transform.SetParent(transform);
                soundAgentGo.transform.localScale = Vector3.one;
                var soundAgent = soundAgentGo.GetOrAddComponent<SoundAgent>();
                soundAgent.Init(this);
                m_SoundAgents.Add(soundAgent);
            }

            /// <summary>
            /// 播放声音。
            /// </summary>
            /// <param name="playSoundInfo">播放时的声音信息。</param>
            /// <param name="errorCode">播放过程中可能出现的错误码。</param>
            /// <returns>用于播放的声音代理。</returns>
            public SoundAgent PlaySound(PlaySoundInfo playSoundInfo, out EPlaySoundErrorCode? errorCode)
            {
                errorCode = null;
                SoundAgent candidateAgent = null; // 候选播放代理
                
                if (playSoundInfo is null) return null;
                

                // 遍历所有声音播放代理，找到合适的代理播放声音
                foreach (var soundAgent in m_SoundAgents)
                {
                    // 1.如果存在没有在播放声音的代理，则将其作为候选代理，并跳出查找。
                    if (!soundAgent.IsPlaying)
                    {
                        candidateAgent = soundAgent;
                        break;
                    }

                    // 2.所有的代理都在播放声音，则找到优先级较低的代理，将其设置为候选代理
                    if (soundAgent.Priority < playSoundInfo.SoundParams.Priority)
                    {
                        if (!candidateAgent || soundAgent.Priority < candidateAgent.Priority)
                            candidateAgent = soundAgent;
                        break;
                    }

                    // 3.所有的代理都在播放声音，且找不到优先级较低的代理，则判断声音组中的声音是否设置了允许被同优先级声音替换，如果允许，则使用同优先级的代理作为候选代理。
                    if (AllowBeReplacedBySamePriority && soundAgent.Priority == playSoundInfo.SoundParams.Priority)
                    {
                        if (!candidateAgent || soundAgent.SetSoundAssetTime < candidateAgent.SetSoundAssetTime)
                            candidateAgent = soundAgent;
                    }
                }

                if (!candidateAgent)
                {
                    errorCode = EPlaySoundErrorCode.IgnoredBecauseLowPriority;
                    return null;
                }

                if (!candidateAgent.SetSoundAsset(playSoundInfo.SoundAsset))
                {
                    errorCode = EPlaySoundErrorCode.SetSoundAssetFailure;
                    return null;
                }

                candidateAgent.SerialId           = playSoundInfo.SerialId;
                candidateAgent.Time               = playSoundInfo.SoundParams.Time;
                candidateAgent.Loop               = playSoundInfo.SoundParams.Loop;
                candidateAgent.Pitch              = playSoundInfo.SoundParams.Pitch;
                candidateAgent.Priority           = playSoundInfo.SoundParams.Priority;
                candidateAgent.PanStereo          = playSoundInfo.SoundParams.PanStereo;
                candidateAgent.MaxDistance        = playSoundInfo.SoundParams.MaxDistance;
                candidateAgent.SpatialBlend       = playSoundInfo.SoundParams.SpatialBlend;
                candidateAgent.DopplerLevel       = playSoundInfo.SoundParams.DopplerLevel;
                candidateAgent.MuteInSoundGroup   = playSoundInfo.SoundParams.IsMute;
                candidateAgent.VolumeInSoundGroup = playSoundInfo.SoundParams.Volume;

                // 使用代理播放声音
                candidateAgent.Play(playSoundInfo.SoundAssetPath, playSoundInfo.SoundParams.FadeInSeconds, playSoundInfo.OnPlayEnd);
                return candidateAgent;
            }

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            /// <param name="serialId">要停止播放声音的序列编号。</param>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            /// <returns>是否停止播放声音成功。</returns>
            public bool StopSound(int serialId, float fadeOutSeconds)
            {
                foreach (var soundAgent in m_SoundAgents.Where(soundAgent => soundAgent.SerialId == serialId))
                {
                    soundAgent.Stop(fadeOutSeconds);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            /// <param name="serialId">要暂停播放声音的序列编号。</param>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            /// <returns>是否暂停播放声音成功。</returns>
            public bool PauseSound(int serialId, float fadeOutSeconds)
            {
                foreach (var soundAgent in m_SoundAgents.Where(soundAgent => soundAgent.SerialId == serialId))
                {
                    soundAgent.Pause(fadeOutSeconds);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            /// <param name="serialId">要恢复播放声音的序列编号。</param>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            /// <returns>是否恢复播放声音成功。</returns>
            public bool ResumeSound(int serialId, float fadeInSeconds)
            {
                foreach (var soundAgent in m_SoundAgents.Where(soundAgent => soundAgent.SerialId == serialId))
                {
                    soundAgent.Resume(fadeInSeconds);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 停止所有已加载的声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void StopAllLoadedSounds(float fadeOutSeconds)
            {
                foreach (var soundAgent in m_SoundAgents.Where(soundAgent => soundAgent.IsPlaying))
                {
                    soundAgent.Stop(fadeOutSeconds);
                }
            }
        }
    }
}