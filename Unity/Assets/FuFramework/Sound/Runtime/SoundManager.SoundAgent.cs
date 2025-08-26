using System;
using UnityEngine;
using System.Collections;
using FuFramework.Asset.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entity.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    public partial class SoundManager
    {
        /// <summary>
        /// 声音播放代理。
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

            /// <summary>
            /// 播放声音的AudioSource组件
            /// </summary>
            private AudioSource m_AudioSource;

            /// <summary>
            /// 声音绑定的实体
            /// </summary>
            private EntityLogic m_BindingEntityLogic;

            /// <summary>
            /// 暂停时音量
            /// </summary>
            private float m_VolumeWhenPause;

            /// <summary>
            /// 应用是否处于暂停状态
            /// </summary>
            private bool m_IsAppPause;

            /// <summary>
            /// 正常播放完成的回调
            /// </summary>
            private Action m_OnPlayEnd;


            /// <summary>
            /// 获取或设置声音的序列编号。
            /// </summary>
            public int SerialId { get; set; }

            /// <summary>
            /// 声音资源全路径。
            /// </summary>
            private string SoundAssetPath { get; set; }

            /// <summary>
            /// 获取声音创建时间。
            /// </summary>
            internal DateTime SetSoundAssetTime { get; private set; }


            /// <summary>
            /// 获取或设置播放位置(以秒为单位)。
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
            /// 获取当前是否正在播放。
            /// </summary>
            public bool IsPlaying => m_AudioSource.isPlaying;

            /// <summary>
            /// 获取声音长度。
            /// </summary>
            public float Length => m_AudioSource.clip ? m_AudioSource.clip.length : 0f;


            /// <summary>
            /// 初始化声音代理的新实例。
            /// </summary>
            /// <param name="soundGroup">所在的声音组。</param>
            /// <param name="manager">声音管理器</param>
            public void Init(SoundGroup soundGroup, SoundManager manager)
            {
                FuGuard.NotNull(soundGroup, nameof(soundGroup));
                FuGuard.NotNull(manager,    nameof(manager));
                Reset();
                m_SoundGroup   = soundGroup;
                SerialId       = 0;
                SoundAssetPath = null;
                m_SoundAsset   = null;
            }

            private void Awake()
            {
                m_AudioSource             = gameObject.GetOrAddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.rolloffMode = AudioRolloffMode.Custom;
            }

            private void Update()
            {
                // 应用没有暂停，且声音没有播放，且声音资源存在，且播放位置大于等于声音长度，说明播放完成，则重置声音相关设置
                if (!m_IsAppPause && !IsPlaying && m_AudioSource.clip && Time >= Length)
                {
                    Log.Info($"声音 '{m_AudioSource.clip.name}' 播放完成!");
                    m_OnPlayEnd?.Invoke();
                    Reset();
                    return;
                }

                // 声音绑定的实体存在，则更新声音位置
                if (m_BindingEntityLogic)
                    UpdateAgentPosition();
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

                transform.position = m_BindingEntityLogic.CachedTransform.position;
            }

            /// <summary>
            /// 设置声音资源。
            /// </summary>
            /// <param name="soundAsset">声音资源。</param>
            /// <returns>是否设置声音资源成功。</returns>
            internal bool SetSoundAsset(object soundAsset)
            {
                Reset();
                m_SoundAsset      = soundAsset;
                SetSoundAssetTime = DateTime.UtcNow;

                var audioClip = soundAsset as AudioClip;
                if (!audioClip) return false;
                m_AudioSource.clip = audioClip;
                return true;
            }

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
            /// <param name="wPos">声音所在的世界坐标。</param>
            public void SetWorldPosition(Vector3 wPos) => transform.position = wPos;

            /// <summary>
            /// 播放声音。
            /// </summary>
            /// <param name="assetPath">声音资源路径。</param>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            /// <param name="onPlayEnd"></param>
            public void Play(string assetPath, float fadeInSeconds, Action onPlayEnd = null)
            {
                StopAllCoroutines();
                m_AudioSource.Play();
                SoundAssetPath = assetPath;
                m_OnPlayEnd    = onPlayEnd;

                // 声音淡入
                if (fadeInSeconds <= 0f) return;
                var volume = m_AudioSource.volume;
                m_AudioSource.volume = 0f;
                StartCoroutine(FadeToVolume(m_AudioSource, volume, fadeInSeconds));
            }

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
                    AssetManager.Instance.UnloadAsset(SoundAssetPath);
                    m_SoundAsset = null;
                }

                transform.localPosition = Vector3.zero;
                m_AudioSource.clip      = null;
                m_BindingEntityLogic    = null;
                m_VolumeWhenPause       = 0f;
                m_OnPlayEnd             = null;

                SetSoundAssetTime  = DateTime.MinValue;
                Time               = 0;
                Pitch              = 1;
                Loop               = false;
                Priority           = 0;
                PanStereo          = 0;
                SpatialBlend       = 0;
                DopplerLevel       = 1;
                MaxDistance        = 100;
                VolumeInSoundGroup = 1;
                MuteInSoundGroup   = false;
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
            /// 声音渐入协程。
            /// </summary>
            /// <param name="audioSource"></param>
            /// <param name="volume"></param>
            /// <param name="duration"></param>
            /// <returns></returns>
            private IEnumerator FadeToVolume(AudioSource audioSource, float volume, float duration)
            {
                var time           = 0f;
                var originalVolume = audioSource.volume;
                while (time < duration)
                {
                    time               += UnityEngine.Time.deltaTime;
                    audioSource.volume =  Mathf.Lerp(originalVolume, volume, time / duration);
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

            /// <summary>
            /// 应用暂停/恢复时(进入后台/回到前台)时，设置标志位，暂停/恢复播放声音。
            /// </summary>
            /// <param name="isPause"></param>
            private void OnApplicationPause(bool isPause)
            {
                m_IsAppPause = isPause;
                if (isPause)
                    Pause(0);
                else
                    Resume(0);
            }
        }
    }
}