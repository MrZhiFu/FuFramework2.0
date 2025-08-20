using System;
using System.Collections;
using FuFramework.Core.Runtime;
using FuFramework.Entity.Runtime;
using UnityEngine;
using UnityEngine.Audio;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
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
    public class DefaultSoundAgentHelper : SoundAgentHelperBase
    {
        private Transform   m_CachedTransform;    // 缓存Transform组件
        private AudioSource m_AudioSource;        // 播放声音的AudioSource组件
        private EntityLogic m_BindingEntityLogic; // 声音绑定的实体

        private float m_VolumeWhenPause;      // 暂停时音量
        private bool  m_ApplicationPauseFlag; // 是否处于应用暂停状态

        private EventHandler<ResetSoundAgentEventArgs> m_ResetSoundAgentEventHandler; // 声音资源重置事件

        /// <summary>
        /// 获取当前是否正在播放。
        /// </summary>
        public override bool IsPlaying => m_AudioSource.isPlaying;

        /// <summary>
        /// 获取声音长度。
        /// </summary>
        public override float Length => m_AudioSource.clip ? m_AudioSource.clip.length : 0f;

        /// <summary>
        /// 获取或设置播放位置。
        /// </summary>
        public override float Time
        {
            get => m_AudioSource.time;
            set => m_AudioSource.time = value;
        }

        /// <summary>
        /// 获取或设置是否静音。
        /// </summary>
        public override bool Mute
        {
            get => m_AudioSource.mute;
            set => m_AudioSource.mute = value;
        }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public override bool Loop
        {
            get => m_AudioSource.loop;
            set => m_AudioSource.loop = value;
        }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public override int Priority
        {
            get => 128 - m_AudioSource.priority;
            set => m_AudioSource.priority = 128 - value;
        }

        /// <summary>
        /// 获取或设置音量大小。
        /// </summary>
        public override float Volume
        {
            get => m_AudioSource.volume;
            set => m_AudioSource.volume = value;
        }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public override float Pitch
        {
            get => m_AudioSource.pitch;
            set => m_AudioSource.pitch = value;
        }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public override float PanStereo
        {
            get => m_AudioSource.panStereo;
            set => m_AudioSource.panStereo = value;
        }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public override float SpatialBlend
        {
            get => m_AudioSource.spatialBlend;
            set => m_AudioSource.spatialBlend = value;
        }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public override float MaxDistance
        {
            get => m_AudioSource.maxDistance;
            set => m_AudioSource.maxDistance = value;
        }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public override float DopplerLevel
        {
            get => m_AudioSource.dopplerLevel;
            set => m_AudioSource.dopplerLevel = value;
        }

        /// <summary>
        /// 获取或设置声音代理辅助器所在的混音组。
        /// </summary>
        public override AudioMixerGroup AudioMixerGroup
        {
            get => m_AudioSource.outputAudioMixerGroup;
            set => m_AudioSource.outputAudioMixerGroup = value;
        }

        /// <summary>
        /// 重置声音代理事件。
        /// </summary>
        public override event EventHandler<ResetSoundAgentEventArgs> ResetSoundAgent
        {
            add => m_ResetSoundAgentEventHandler += value;
            remove => m_ResetSoundAgentEventHandler -= value;
        }

        private void Awake()
        {
            m_CachedTransform         = transform;
            m_AudioSource             = gameObject.GetOrAddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.rolloffMode = AudioRolloffMode.Custom;
        }

        private void Update()
        {
            // 应用没有暂停，且声音没有播放，且声音资源存在，则触发声音资源重置事件
            if (!m_ApplicationPauseFlag && !IsPlaying && m_AudioSource.clip && m_ResetSoundAgentEventHandler != null)
            {
                var resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
                m_ResetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
                ReferencePool.Release(resetSoundAgentEventArgs);
                return;
            }

            if (m_BindingEntityLogic)
            {
                UpdateAgentPosition();
            }
        }

        private void OnApplicationPause(bool pause) => m_ApplicationPauseFlag = pause;

        /// <summary>
        /// 随着绑定的实体位置更新声音位置。
        /// </summary>
        private void UpdateAgentPosition()
        {
            if (m_BindingEntityLogic.Available)
            {
                m_CachedTransform.position = m_BindingEntityLogic.CachedTransform.position;
                return;
            }

            if (m_ResetSoundAgentEventHandler != null)
            {
                var resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
                m_ResetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
                ReferencePool.Release(resetSoundAgentEventArgs);
            }
        }


        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public override void Play(float fadeInSeconds)
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
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public override void Stop(float fadeOutSeconds)
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
        public override void Pause(float fadeOutSeconds)
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
        public override void Resume(float fadeInSeconds)
        {
            StopAllCoroutines();
            m_AudioSource.UnPause();
            if (fadeInSeconds > 0f)
                StartCoroutine(FadeToVolume(m_AudioSource, m_VolumeWhenPause, fadeInSeconds));
            else
                m_AudioSource.volume = m_VolumeWhenPause;
        }

        /// <summary>
        /// 重置声音代理辅助器。
        /// </summary>
        public override void Reset()
        {
            m_CachedTransform.localPosition = Vector3.zero;
            m_AudioSource.clip              = null;
            m_BindingEntityLogic            = null;
            m_VolumeWhenPause               = 0f;
        }

        /// <summary>
        /// 设置声音资源。
        /// </summary>
        /// <param name="soundAsset">声音资源。</param>
        /// <returns>是否设置声音资源成功。</returns>
        public override bool SetSoundAsset(object soundAsset)
        {
            var audioClip = soundAsset as AudioClip;
            if (!audioClip) return false;
            m_AudioSource.clip = audioClip;
            return true;
        }

        /// <summary>
        /// 设置声音绑定的实体。
        /// </summary>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        public override void SetBindingEntity(Entity.Runtime.Entity bindingEntity)
        {
            m_BindingEntityLogic = bindingEntity.Logic;
            if (m_BindingEntityLogic)
            {
                UpdateAgentPosition();
                return;
            }

            if (m_ResetSoundAgentEventHandler == null) return;

            var resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
            m_ResetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
            ReferencePool.Release(resetSoundAgentEventArgs);
        }

        /// <summary>
        /// 设置声音所在的世界坐标。
        /// </summary>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        public override void SetWorldPosition(Vector3 worldPosition)
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
    }
}