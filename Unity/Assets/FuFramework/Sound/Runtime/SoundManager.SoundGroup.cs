using System.Collections.Generic;
using System.Linq;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    public partial class SoundManager
    {
        /// <summary>
        /// 声音组。
        /// 功能:管理该组中的声音, 包括播放、停止、暂停、恢复等。
        /// </summary>
        public class SoundGroup
        {
            private readonly List<SoundAgent> m_SoundAgents; // 声音播放代理列表

            private bool  m_Mute;   // 是否静音。
            private float m_Volume; // 声音组音量。

            /// <summary>
            /// 获取声音组名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取声音组辅助器。
            /// </summary>
            public DefaultSoundGroupHelper Helper { get; }

            /// <summary>
            /// 获取或设置声音组中的声音是否允许被同优先级声音替换。
            /// </summary>
            public bool AllowBeReplacedBySamePriority { get; set; } = true;

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
                    m_Mute = value;
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
                    m_Volume = value;
                    foreach (var soundAgent in m_SoundAgents)
                    {
                        soundAgent.RefreshVolume();
                    }
                }
            }

            /// <summary>
            /// 初始化声音组的新实例。
            /// </summary>
            /// <param name="name">声音组名称。</param>
            /// <param name="soundGroupHelper">声音组辅助器。</param>
            public SoundGroup(string name, DefaultSoundGroupHelper soundGroupHelper)
            {
                if (string.IsNullOrEmpty(name)) throw new FuException("[SoundGroup]声音组名称不能为空!");

                Name          = name;
                Helper        = soundGroupHelper ?? throw new FuException("[SoundGroup]声音组辅助器不能为空!.");
                m_SoundAgents = new List<SoundAgent>();
            }

            /// <summary>
            /// 增加声音代理辅助器。
            /// </summary>
            /// <param name="soundAgentHelper">要增加的声音代理辅助器。</param>
            /// <param name="manager">声音管理器。</param>
            public void AddSoundAgentHelper(DefaultSoundAgentHelper soundAgentHelper, SoundManager manager)
            {
                m_SoundAgents.Add(new SoundAgent(this, soundAgentHelper, manager));
            }

            /// <summary>
            /// 播放声音。
            /// </summary>
            /// <param name="serialId">声音的序列编号。</param>
            /// <param name="soundAsset">声音资源。</param>
            /// <param name="playSoundParams">播放声音参数。</param>
            /// <param name="errorCode">错误码。</param>
            /// <returns>用于播放的声音代理。</returns>
            public SoundAgent PlaySound(int serialId, object soundAsset, PlaySoundParams playSoundParams, out EPlaySoundErrorCode? errorCode)
            {
                errorCode = null;
                SoundAgent candidateAgent = null; // 候选播放代理

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
                    if (soundAgent.Priority < playSoundParams.Priority)
                    {
                        if (candidateAgent == null || soundAgent.Priority < candidateAgent.Priority) 
                            candidateAgent = soundAgent;
                        break;
                    }

                    // 3.所有的代理都在播放声音，且找不到优先级较低的代理，则判断声音组中的声音是否设置了允许被同优先级声音替换，如果允许，则使用同优先级的代理作为候选代理。
                    if (AllowBeReplacedBySamePriority && soundAgent.Priority == playSoundParams.Priority)
                    {
                        if (candidateAgent == null || soundAgent.SetSoundAssetTime < candidateAgent.SetSoundAssetTime) 
                            candidateAgent = soundAgent;
                    }
                }

                if (candidateAgent == null)
                {
                    errorCode = EPlaySoundErrorCode.IgnoredDueToLowPriority;
                    return null;
                }

                if (!candidateAgent.SetSoundAsset(soundAsset))
                {
                    errorCode = EPlaySoundErrorCode.SetSoundAssetFailure;
                    return null;
                }

                candidateAgent.SerialId           = serialId;
                candidateAgent.Time               = playSoundParams.Time;
                candidateAgent.MuteInSoundGroup   = playSoundParams.IsMute;
                candidateAgent.Loop               = playSoundParams.Loop;
                candidateAgent.Priority           = playSoundParams.Priority;
                candidateAgent.VolumeInSoundGroup = playSoundParams.Volume;
                candidateAgent.Pitch              = playSoundParams.Pitch;
                candidateAgent.PanStereo          = playSoundParams.PanStereo;
                candidateAgent.SpatialBlend       = playSoundParams.SpatialBlend;
                candidateAgent.MaxDistance        = playSoundParams.MaxDistance;
                candidateAgent.DopplerLevel       = playSoundParams.DopplerLevel;
                
                // 使用代理播放声音
                candidateAgent.Play(playSoundParams.FadeInSeconds);
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
            public void StopAllLoadedSounds()
            {
                foreach (var soundAgent in m_SoundAgents.Where(soundAgent => soundAgent.IsPlaying))
                {
                    soundAgent.Stop();
                }
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