using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Entity;

namespace Hotfix.Framework.Sound
{
	public partial class SoundModule
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
			/// 资源管理模块。
			/// </summary>
			private AssetModule m_assetModule;

			/// <summary>
			/// 所在的声音组。
			/// </summary>
			private SoundGroup m_soundGroup;

			/// <summary>
			/// 声音资源。
			/// </summary>
			private object m_soundAsset;

			/// <summary>
			/// 声音资源句柄。随播放持有，Reset 时释放（先于 UnloadAsset，使引用计数归零后可真正卸载）。
			/// 同一路径并发播放时各自持有句柄，互不影响。
			/// </summary>
			private AssetHandle m_soundAssetHandle;

			/// <summary>
			/// 在声音组内是否静音。
			/// </summary>
			private bool m_muteInSoundGroup;

			/// <summary>
			/// 在声音组内音量大小。
			/// </summary>
			private float m_volumeInSoundGroup;

			/// <summary>
			/// 播放声音的AudioSource组件
			/// </summary>
			private AudioSource m_audioSource;

			/// <summary>
			/// 声音绑定的实体
			/// </summary>
			private EntityLogic m_bindingEntityLogic;

			/// <summary>
			/// 暂停时音量
			/// </summary>
			private float m_volumeWhenPause;

			/// <summary>
			/// 应用是否处于暂停状态
			/// </summary>
			private bool m_isAppPause;

			/// <summary>
			/// 正常播放完成的回调
			/// </summary>
			private Action m_onPlayEnd;

			/// <summary>
			/// 在途音量渐变的取消源。每次发起渐变时重建；被新的渐变/停止/暂停/重置/销毁打断时取消并释放。
			/// </summary>
			private CancellationTokenSource m_fadeCts;


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
				get => m_audioSource.time;
				set => m_audioSource.time = value;
			}

			/// <summary>
			/// 获取或设置是否静音。
			/// </summary>
			public bool Mute
			{
				get => m_audioSource.mute;
				set => m_audioSource.mute = value;
			}

			/// <summary>
			/// 获取或设置是否循环播放。
			/// </summary>
			public bool Loop
			{
				get => m_audioSource.loop;
				set => m_audioSource.loop = value;
			}

			/// <summary>
			/// 获取或设置声音优先级。
			/// </summary>
			public int Priority
			{
				get => 128 - m_audioSource.priority;
				set => m_audioSource.priority = 128 - value;
			}

			/// <summary>
			/// 获取或设置音量大小。
			/// </summary>
			public float Volume
			{
				get => m_audioSource.volume;
				set => m_audioSource.volume = value;
			}

			/// <summary>
			/// 获取或设置声音音调。
			/// </summary>
			public float Pitch
			{
				get => m_audioSource.pitch;
				set => m_audioSource.pitch = value;
			}

			/// <summary>
			/// 获取或设置声音立体声声相。
			/// </summary>
			public float PanStereo
			{
				get => m_audioSource.panStereo;
				set => m_audioSource.panStereo = value;
			}

			/// <summary>
			/// 获取或设置声音空间混合量。
			/// </summary>
			public float SpatialBlend
			{
				get => m_audioSource.spatialBlend;
				set => m_audioSource.spatialBlend = value;
			}

			/// <summary>
			/// 获取或设置声音最大距离。
			/// </summary>
			public float MaxDistance
			{
				get => m_audioSource.maxDistance;
				set => m_audioSource.maxDistance = value;
			}

			/// <summary>
			/// 获取或设置声音多普勒等级。
			/// </summary>
			public float DopplerLevel
			{
				get => m_audioSource.dopplerLevel;
				set => m_audioSource.dopplerLevel = value;
			}

			/// <summary>
			/// 获取或设置在声音组内是否静音。
			/// </summary>
			public bool MuteInSoundGroup
			{
				get => m_muteInSoundGroup;
				set
				{
					m_muteInSoundGroup = value;
					RefreshMute();
				}
			}

			/// <summary>
			/// 获取或设置在声音组内音量大小。
			/// </summary>
			public float VolumeInSoundGroup
			{
				get => m_volumeInSoundGroup;
				set
				{
					m_volumeInSoundGroup = value;
					RefreshVolume();
				}
			}


			/// <summary>
			/// 获取当前是否正在播放。
			/// </summary>
			public bool IsPlaying => m_audioSource && m_audioSource.isPlaying;

			/// <summary>
			/// 获取声音长度。
			/// </summary>
			public float Length => m_audioSource && m_audioSource.clip ? m_audioSource.clip.length : 0f;


			/// <summary>
			/// 初始化声音代理的新实例。
			/// </summary>
			/// <param name="soundGroup">所在的声音组。</param>
			public void Init(SoundGroup soundGroup)
			{
				soundGroup.NotNull(nameof(soundGroup));
				m_assetModule = ModuleManager.GetModule<AssetModule>();

				m_soundGroup       = soundGroup;
				SerialId           = 0;
				SoundAssetPath     = null;
				m_soundAsset       = null;
				m_soundAssetHandle = null;
				Reset();
			}

			private void Awake()
			{
				m_audioSource             = gameObject.GetOrAddComponent<AudioSource>();
				m_audioSource.playOnAwake = false;
				m_audioSource.rolloffMode = AudioRolloffMode.Custom;
			}

			private void Update()
			{
				// 应用没有暂停，且声音没有播放，且声音资源存在，且播放位置大于等于声音长度，说明播放完成，则重置声音相关设置
				if (!m_isAppPause && !IsPlaying && m_audioSource.clip && Time >= Length)
				{
					FuLogger.LogInfo($"[SoundModule.SoundAgent] 声音 '{m_audioSource.clip.name}' 播放完成!");
					try
					{
						m_onPlayEnd?.Invoke();
					}
					finally
					{
						Reset(); // 回调抛异常也必须释放句柄/资源，否则句柄泄漏且每帧重复回调
					}

					return;
				}

				// 声音绑定的实体存在，则更新声音位置
				if (m_bindingEntityLogic)
					UpdateAgentPosition();
			}

			/// <summary>
			/// 随着绑定的实体位置更新声音位置。
			/// </summary>
			private void UpdateAgentPosition()
			{
				if (!m_bindingEntityLogic.Available)
				{
					Reset();
					return;
				}

				transform.position = m_bindingEntityLogic.CachedTransform.position;
			}

			/// <summary>
			/// 设置声音资源。
			/// </summary>
			/// <param name="soundAsset">声音资源。</param>
			/// <param name="soundAssetHandle">声音资源句柄（随播放持有，Reset 时释放）。</param>
			/// <returns>是否设置声音资源成功。</returns>
			internal bool SetSoundAsset(object soundAsset, AssetHandle soundAssetHandle)
			{
				Reset();
				// 先校验资源类型：非 AudioClip 时不设置状态（m_soundAsset/m_soundAssetHandle 保持 null），
				// 避免后续 Reset 因 m_soundAsset 非空而调 UnloadAsset(null) 抛异常；句柄由调用方（SoundGroup）释放。
				var audioClip = soundAsset as AudioClip;
				if (!audioClip) return false;

				m_soundAsset       = soundAsset;
				m_soundAssetHandle = soundAssetHandle;
				SetSoundAssetTime  = DateTime.UtcNow;
				m_audioSource.clip = audioClip;
				return true;
			}

			/// <summary>
			/// 设置声音绑定的实体。
			/// </summary>
			/// <param name="bindingEntity">声音绑定的实体。</param>
			public void SetBindingEntity(Entity.Entity bindingEntity)
			{
				m_bindingEntityLogic = bindingEntity.Logic;
				if (!m_bindingEntityLogic)
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
				var fadeToken = BeginFade();
				m_audioSource.Play();
				SoundAssetPath = assetPath;
				m_onPlayEnd    = onPlayEnd;

				// 声音淡入
				if (fadeInSeconds <= 0f) return;
				var volume = m_audioSource.volume;
				m_audioSource.volume = 0f;
				FadeInAsync(volume, fadeInSeconds, fadeToken).Forget();
			}

			/// <summary>
			/// 停止播放声音。
			/// </summary>
			/// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
			public void Stop(float fadeOutSeconds)
			{
				var fadeToken = BeginFade();
				if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
					FadeOutThenStopAsync(fadeOutSeconds, fadeToken).Forget();
				else
				{
					m_audioSource.Stop();
					Reset(); // 停止后释放资源句柄，避免 bundle 残留（否则句柄持有到 agent 复用）
				}
			}

			/// <summary>
			/// 暂停播放声音。
			/// </summary>
			/// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
			public void Pause(float fadeOutSeconds)
			{
				var fadeToken = BeginFade();
				m_volumeWhenPause = m_audioSource.volume;
				if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
					FadeOutThenPauseAsync(fadeOutSeconds, fadeToken).Forget();
				else
					m_audioSource.Pause();
			}

			/// <summary>
			/// 恢复播放声音。
			/// </summary>
			/// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
			public void Resume(float fadeInSeconds)
			{
				var fadeToken = BeginFade();
				m_audioSource.UnPause();
				if (fadeInSeconds > 0f)
					FadeInAsync(m_volumeWhenPause, fadeInSeconds, fadeToken).Forget();
				else
					m_audioSource.volume = m_volumeWhenPause;
			}

			/// <summary>
			/// 重置声音代理。
			/// </summary>
			public void Reset()
			{
				// 取消在途音量渐变：否则其续体会在 Reset 之后继续写 AudioSource.volume
				// （原协程由 StopAllCoroutines 保证，改写为 UniTask 后需显式取消）
				CancelFade();

				// 先释放句柄再卸载资源（托管操作，即使组件已被 Unity teardown 销毁也执行）：
				// 句柄不释放则 provider.RefCount 不为 0，UnloadAsset 的 TryUnloadUnusedAsset 永不生效
				if (m_soundAssetHandle != null)
				{
					m_soundAssetHandle.Release();
					m_soundAssetHandle = null;
				}

				if (m_soundAsset != null)
				{
					m_assetModule.UnloadAsset(SoundAssetPath);
					m_soundAsset = null;
				}
				SoundAssetPath = null; // 清理陈旧路径，避免后续误用
				SerialId       = 0;    // 清除序列编号：否则 StopSound(serialId) 会误匹配到已重置（未在播放）的代理

				// Unity 停止 Play 时组件可能已被 teardown 销毁：防御后续 Unity 对象访问（transform/AudioSource），避免"对象已销毁仍访问"警告
				if (this == null) return;

				transform.localPosition = Vector3.zero;
				m_audioSource.clip      = null;
				m_bindingEntityLogic    = null;
				m_volumeWhenPause       = 0f;
				m_onPlayEnd             = null;

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
			internal void RefreshMute() => Mute = m_soundGroup.Mute || m_muteInSoundGroup;

			/// <summary>
			/// 刷新音量设置。
			/// </summary>
			internal void RefreshVolume() => Volume = m_soundGroup.Volume * m_volumeInSoundGroup;


			/// <summary>
			/// 取消在途音量渐变（若有）并释放其取消源。
			/// </summary>
			private void CancelFade()
			{
				if (m_fadeCts == null) return;

				m_fadeCts.Cancel();
				m_fadeCts.Dispose();
				m_fadeCts = null;
			}

			/// <summary>
			/// 开始一次新的音量渐变：先取消上一次在途渐变，再为其建立独立的取消源。
			/// </summary>
			/// <returns>本次渐变的取消令牌。</returns>
			private CancellationToken BeginFade()
			{
				CancelFade();
				m_fadeCts = new CancellationTokenSource();
				return m_fadeCts.Token;
			}

			/// <summary>
			/// 音量渐变本体：每帧补一档（与原 WaitForEndOfFrame 的节奏一致）。
			/// 被取消时抛 OperationCanceledException，由调用方决定是否改变播放状态。
			/// </summary>
			/// <param name="audioSource">目标音源。</param>
			/// <param name="volume">目标音量。</param>
			/// <param name="duration">渐变时长（秒）。</param>
			/// <param name="token">本次渐变的取消令牌。</param>
			private async UniTask FadeToVolumeAsync(AudioSource audioSource, float volume, float duration, CancellationToken token)
			{
				var time           = 0f;
				var originalVolume = audioSource.volume;
				while (time < duration)
				{
					time               += UnityEngine.Time.deltaTime;
					audioSource.volume =  Mathf.Lerp(originalVolume, volume, time / duration);
					await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
				}

				audioSource.volume = volume;
			}

			/// <summary>
			/// 播放/恢复时的渐入：被取消则静默结束（由新的渐变/停止/重置接管音量）。
			/// </summary>
			/// <param name="volume">目标音量。</param>
			/// <param name="duration">渐变时长（秒）。</param>
			/// <param name="token">本次渐变的取消令牌。</param>
			private async UniTaskVoid FadeInAsync(float volume, float duration, CancellationToken token)
			{
				try
				{
					await FadeToVolumeAsync(m_audioSource, volume, duration, token);
				}
				catch (OperationCanceledException)
				{
					// 预期路径：被新的渐变/停止/暂停/重置/销毁打断，不做善后
				}
			}

			/// <summary>
			/// 停止时的渐出：淡出完成后停止播放并释放资源句柄；被取消则不改变播放状态。
			/// </summary>
			/// <param name="fadeOutSeconds">淡出时长（秒）。</param>
			/// <param name="token">本次渐变的取消令牌。</param>
			private async UniTaskVoid FadeOutThenStopAsync(float fadeOutSeconds, CancellationToken token)
			{
				try
				{
					await FadeToVolumeAsync(m_audioSource, 0f, fadeOutSeconds, token);
				}
				catch (OperationCanceledException)
				{
					return;
				}

				m_audioSource.Stop();
				Reset(); // 淡出完成后释放资源句柄
			}

			/// <summary>
			/// 暂停时的渐出：淡出完成后暂停；被取消则不改变播放状态。
			/// </summary>
			/// <param name="fadeOutSeconds">淡出时长（秒）。</param>
			/// <param name="token">本次渐变的取消令牌。</param>
			private async UniTaskVoid FadeOutThenPauseAsync(float fadeOutSeconds, CancellationToken token)
			{
				try
				{
					await FadeToVolumeAsync(m_audioSource, 0f, fadeOutSeconds, token);
				}
				catch (OperationCanceledException)
				{
					return;
				}

				m_audioSource.Pause();
			}

			/// <summary>
			/// 组件销毁时取消在途渐变：原协程随对象销毁自动中止，改写为 UniTask 后需显式取消，
			/// 否则续体会在已销毁的 AudioSource 上继续访问。
			/// </summary>
			private void OnDestroy() => CancelFade();

			/// <summary>
			/// 应用暂停/恢复时(进入后台/回到前台)时，设置标志位，暂停/恢复播放声音。
			/// </summary>
			/// <param name="isPause"></param>
			private void OnApplicationPause(bool isPause)
			{
				m_isAppPause = isPause;
				if (isPause)
					Pause(0);
				else
					Resume(0);
			}
		}
	}
}
