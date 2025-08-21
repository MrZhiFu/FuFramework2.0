using System;
using System.Collections;
using FuFramework.Core.Runtime;
using FuFramework.Entity.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    public partial class SoundManager
    {
        /// <summary>
        /// 默认声音代理辅助器。
        /// 功能：
        ///     1.使用AudioSource组件，实现了声音播放，暂停，停止，重置，渐入渐出等方法。
        ///     2.提供事件机制，用于通知绑定实体的声音资源发生变化。
        ///     3.提供接口，用于绑定实体的声音资源。
        ///     4.提供接口，用于获取声音的长度，播放位置，是否静音，是否循环播放，声音优先级，音量大小，声音音调，声音立体声声相，
        ///       声音空间混合量，声音最大距离，声音多普勒等级，声音混音组等属性。
        /// </summary>
        public sealed class SoundAgent : MonoBehaviour
        {
            /// <summary>
            /// 所在的声音组。
            /// </summary>
            private SoundGroup m_SoundGroup;

            /// <summary>
            /// 声音管理器
            /// </summary>
            private SoundManager m_SoundManager;

            /// <summary>
            /// 声音资源。
            /// </summary>
            private object m_SoundAsset;

            /// <summary>
            /// 在声音组内是否静音。
            /// </summary>
            private bool m_MuteInSoundGroup;

            /// <summary>
            /// 在声音组内音量大小。
            /// </summary>
            private float m_VolumeInSoundGroup;

            private Transform m_CachedTransform; // 缓存Transform组件
            private AudioSource m_AudioSource; // 播放声音的AudioSource组件
            private EntityLogic m_BindingEntityLogic; // 声音绑定的实体

            private float m_VolumeWhenPause; // 暂停时音量
            private bool m_ApplicationPauseFlag; // 是否处于应用暂停状态

            /// <summary>
            /// 初始化声音代理的新实例。
            /// </summary>
            /// <param name="soundGroup">所在的声音组。</param>
            /// <param name="manager">声音管理器</param>
            public void Init(SoundGroup soundGroup, SoundManager manager)
            {
                FuGuard.NotNull(soundGroup, nameof(soundGroup));
                FuGuard.NotNull(manager, nameof(manager));
                m_SoundGroup = soundGroup;
                m_SoundManager = manager;
                SerialId = 0;
                m_SoundAsset = null;
                Reset();
            }

            /// <summary>
            /// 获取所在的声音组。
            /// </summary>
            public SoundGroup SoundGroup => m_SoundGroup;

            /// <summary>
            /// 获取或设置声音的序列编号。
            /// </summary>
            public int SerialId { get; set; }

            /// <summary>
            /// 获取当前是否正在播放。
            /// </summary>
            public bool IsPlaying => m_AudioSource.isPlaying;

            /// <summary>
            /// 获取声音长度。
            /// </summary>
            public float Length => m_AudioSource.clip ? m_AudioSource.clip.length : 0f;

            /// <summary>
            /// 获取或设置播放位置。
            /// </summary>
            public float Time
            {
                get => m_AudioSource.time;
                set => m_AudioSource.time = value;
            }

            /// <summary>
            /// 获取或设置是否静音。
            /// </summary>
            public bool Mute
            {
                get => m_AudioSource.mute;
                set => m_AudioSource.mute = value;
            }

            /// <summary>
            /// 获取或设置是否循环播放。
            /// </summary>
            public bool Loop
            {
                get => m_AudioSource.loop;
                set => m_AudioSource.loop = value;
            }

            /// <summary>
            /// 获取或设置声音优先级。
            /// </summary>
            public int Priority
            {
                get => 128 - m_AudioSource.priority;
                set => m_AudioSource.priority = 128 - value;
            }

            /// <summary>
            /// 获取或设置音量大小。
            /// </summary>
            public float Volume
            {
                get => m_AudioSource.volume;
                set => m_AudioSource.volume = value;
            }

            /// <summary>
            /// 获取或设置声音音调。
            /// </summary>
            public float Pitch
            {
                get => m_AudioSource.pitch;
                set => m_AudioSource.pitch = value;
            }

            /// <summary>
            /// 获取或设置声音立体声声相。
            /// </summary>
            public float PanStereo
            {
                get => m_AudioSource.panStereo;
                set => m_AudioSource.panStereo = value;
            }

            /// <summary>
            /// 获取或设置声音空间混合量。
            /// </summary>
            public float SpatialBlend
            {
                get => m_AudioSource.spatialBlend;
                set => m_AudioSource.spatialBlend = value;
            }

            /// <summary>
            /// 获取或设置声音最大距离。
            /// </summary>
            public float MaxDistance
            {
                get => m_AudioSource.maxDistance;
                set => m_AudioSource.maxDistance = value;
            }

            /// <summary>
            /// 获取或设置声音多普勒等级。
            /// </summary>
            public float DopplerLevel
            {
                get => m_AudioSource.dopplerLevel;
                set => m_AudioSource.dopplerLevel = value;
            }

            /// <summary>
            /// 获取或设置在声音组内是否静音。
            /// </summary>
            public bool MuteInSoundGroup
            {
                get => m_MuteInSoundGroup;
                set
                {
                    m_MuteInSoundGroup = value;
                    RefreshMute();
                }
            }

            /// <summary>
            /// 获取或设置在声音组内音量大小。
            /// </summary>
            public float VolumeInSoundGroup
            {
                get => m_VolumeInSoundGroup;
                set
                {
                    m_VolumeInSoundGroup = value;
                    RefreshVolume();
                }
            }

            /// <summary>
            /// 获取声音创建时间。
            /// </summary>
            internal DateTime SetSoundAssetTime { get; private set; }

            private void Awake()
            {
                m_CachedTransform = transform;
                m_AudioSource = gameObject.GetOrAddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.rolloffMode = AudioRolloffMode.Custom;
            }

            private void Update()
            {
                // 应用没有暂停，且声音没有播放，且声音资源存在，则重置声音设置
                if (!m_ApplicationPauseFlag && !IsPlaying && m_AudioSource.clip)
                {
                    Reset();
                    return;
                }

                if (m_BindingEntityLogic)
                {
                    UpdateAgentPosition();
                }
            }

            /// <summary>
            /// 随着绑定的实体位置更新声音位置。
            /// </summary>
            private void UpdateAgentPosition()
            {
                if (!m_BindingEntityLogic.Available)
                {
                    Reset();
                    return;
                }

                m_CachedTransform.position = m_BindingEntityLogic.CachedTransform.position;
            }


            /// <summary>
            /// 播放声音。
            /// </summary>
            public void Play() => Play(Constant.DefaultFadeInSeconds);

            /// <summary>
            /// 播放声音。
            /// </summary>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            public void Play(float fadeInSeconds)
            {
                StopAllCoroutines();
                m_AudioSource.Play();

                // 声音淡入
                if (fadeInSeconds <= 0f) return;
                var volume = m_AudioSource.volume;
                m_AudioSource.volume = 0f;
                StartCoroutine(FadeToVolume(m_AudioSource, volume, fadeInSeconds));
            }

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            public void Stop() => Stop(Constant.DefaultFadeOutSeconds);

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void Stop(float fadeOutSeconds)
            {
                StopAllCoroutines();
                if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
                    StartCoroutine(StopCo(fadeOutSeconds));
                else
                    m_AudioSource.Stop();
            }


            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            public void Pause() => Pause(Constant.DefaultFadeOutSeconds);

            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void Pause(float fadeOutSeconds)
            {
                StopAllCoroutines();
                m_VolumeWhenPause = m_AudioSource.volume;
                if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
                    StartCoroutine(PauseCo(fadeOutSeconds));
                else
                    m_AudioSource.Pause();
            }

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            public void Resume() => Resume(Constant.DefaultFadeInSeconds);

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            public void Resume(float fadeInSeconds)
            {
                StopAllCoroutines();
                m_AudioSource.UnPause();
                if (fadeInSeconds > 0f)
                    StartCoroutine(FadeToVolume(m_AudioSource, m_VolumeWhenPause, fadeInSeconds));
                else
                    m_AudioSource.volume = m_VolumeWhenPause;
            }

            /// <summary>
            /// 重置声音代理。
            /// </summary>
            public void Reset()
            {
                if (m_SoundAsset != null)
                {
                    var soundName = (m_SoundAsset as AudioClip)?.name;
                    m_SoundManager?.m_assetManager?.UnloadAsset(soundName);
                    m_SoundAsset = null;
                }

                m_CachedTransform.localPosition = Vector3.zero;
                m_AudioSource.clip = null;
                m_BindingEntityLogic = null;
                m_VolumeWhenPause = 0f;

                SetSoundAssetTime = DateTime.MinValue;
                Time = Constant.DefaultTime;
                MuteInSoundGroup = Constant.DefaultMute;
                Loop = Constant.DefaultLoop;
                Priority = Constant.DefaultPriority;
                VolumeInSoundGroup = Constant.DefaultVolume;
                Pitch = Constant.DefaultPitch;
                PanStereo = Constant.DefaultPanStereo;
                SpatialBlend = Constant.DefaultSpatialBlend;
                MaxDistance = Constant.DefaultMaxDistance;
                DopplerLevel = Constant.DefaultDopplerLevel;
            }

            /// <summary>
            /// 设置声音资源。
            /// </summary>
            /// <param name="soundAsset">声音资源。</param>
            /// <returns>是否设置声音资源成功。</returns>
            internal bool SetSoundAsset(object soundAsset)
            {
                Reset();
                m_SoundAsset = soundAsset;
                SetSoundAssetTime = DateTime.UtcNow;

                var audioClip = soundAsset as AudioClip;
                if (!audioClip) return false;
                m_AudioSource.clip = audioClip;
                return true;
            }

            /// <summary>
            /// 刷新静音设置。
            /// </summary>
            internal void RefreshMute() => Mute = m_SoundGroup.Mute || m_MuteInSoundGroup;

            /// <summary>
            /// 刷新音量设置。
            /// </summary>
            internal void RefreshVolume() => Volume = m_SoundGroup.Volume * m_VolumeInSoundGroup;


            /// <summary>
            /// 设置声音绑定的实体。
            /// </summary>
            /// <param name="bindingEntity">声音绑定的实体。</param>
            public void SetBindingEntity(Entity.Runtime.Entity bindingEntity)
            {
                m_BindingEntityLogic = bindingEntity.Logic;
                if (!m_BindingEntityLogic)
                {
                    Reset();
                    return;
                }

                UpdateAgentPosition();
            }

            /// <summary>
            /// 设置声音所在的世界坐标。
            /// </summary>
            /// <param name="worldPosition">声音所在的世界坐标。</param>
            public void SetWorldPosition(Vector3 worldPosition)
            {
                m_CachedTransform.position = worldPosition;
            }

            /// <summary>
            /// 声音渐入协程。
            /// </summary>
            /// <param name="audioSource"></param>
            /// <param name="volume"></param>
            /// <param name="duration"></param>
            /// <returns></returns>
            private IEnumerator FadeToVolume(AudioSource audioSource, float volume, float duration)
            {
                var time = 0f;
                var originalVolume = audioSource.volume;
                while (time < duration)
                {
                    time += UnityEngine.Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(originalVolume, volume, time / duration);
                    yield return new WaitForEndOfFrame();
                }

                audioSource.volume = volume;
            }

            /// <summary>
            /// 停止声音的渐出协程。
            /// </summary>
            /// <param name="fadeOutSeconds"></param>
            /// <returns></returns>
            private IEnumerator StopCo(float fadeOutSeconds)
            {
                yield return FadeToVolume(m_AudioSource, 0f, fadeOutSeconds);
                m_AudioSource.Stop();
            }

            /// <summary>
            /// 暂停声音时的渐出协程。
            /// </summary>
            /// <param name="fadeOutSeconds"></param>
            /// <returns></returns>
            private IEnumerator PauseCo(float fadeOutSeconds)
            {
                yield return FadeToVolume(m_AudioSource, 0f, fadeOutSeconds);
                m_AudioSource.Pause();
            }
        }
    }
}