using UnityEngine;
using Hotfix.Framework.Core;
using Hotfix.Framework.Storage;
using System.Collections.Generic;
using SoundGroupCfg = Hotfix.Game.Config.SoundGroup;

namespace Hotfix.Framework.Sound
{
	public partial class SoundModule
	{
		/// <summary>
		/// 声音组。
		/// 功能：
		///     1. 管理该组中的声音, 包括播放、停止、暂停、恢复等。
		///     2. 提供声音组静音、音量设置等接口。
		/// </summary>
		public class SoundGroup : MonoBehaviour
		{
			/// <summary>
			/// 声音组设置存储键前缀。
			/// </summary>
			private const string STORAGE_KEY_PREFIX = "SoundGroup";

			/// <summary>
			/// 声音播放代理列表
			/// </summary>
			private readonly List<SoundAgent> m_soundAgents = new();

			/// <summary>
			/// 是否静音
			/// </summary>
			private bool m_mute;

			/// <summary>
			/// 声音组音量。
			/// </summary>
			private float m_volume;

			/// <summary>
			/// 获取声音组名称。
			/// </summary>
			public string Name { get; private set; }

			/// <summary>
			/// 获取或设置声音组中的声音是否允许被同优先级声音替换。
			/// </summary>
			public bool AllowBeReplacedBySamePriority { get; private set; }

			/// <summary>
			/// 获取声音代理数。
			/// </summary>
			public int SoundAgentCount => m_soundAgents.Count;

			/// <summary>
			/// 获取或设置声音组静音。
			/// </summary>
			public bool Mute
			{
				get => m_mute;
				set
				{
					if (value == m_mute) return;
					m_mute = value;
					SaveMuteSetting();
					foreach (var soundAgent in m_soundAgents)
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
				get => m_volume;
				set
				{
					if (Mathf.Approximately(value, m_volume)) return;
					m_volume = value;
					SaveVolumeSetting();
					foreach (var soundAgent in m_soundAgents)
					{
						soundAgent.RefreshVolume();
					}
				}
			}

			/// <summary>
			/// 初始化声音组的新实例。
			/// </summary>
			/// <param name="groupInfo">声音组信息。</param>
			public void Init(SoundGroupCfg groupInfo)
			{
				groupInfo.NotNull(nameof(groupInfo));
				Name                          = groupInfo.Id.ToString();
				AllowBeReplacedBySamePriority = groupInfo.AllowBeReplacedBySamePriority;

				// 还原玩家存储的设置；未存储过（首次启动/新增组）时回退配置表默认值。
				// 直接赋字段不走 setter：还原不应触发存储回写（无谓标脏）。
				// 代理创建（下方 AddSoundAgentHelper）经 SoundAgent.Init → Reset → RefreshVolume/RefreshMute
				// 应用组值，故须先设值后建代理。
				m_volume = LoadVolumeSetting(groupInfo.Volume);
				m_mute   = LoadMuteSetting(groupInfo.Mute);

				// 添加声音组辅助器中的声音播放代理辅助器
				for (var i = 0; i < groupInfo.AgentCount; i++)
				{
					AddSoundAgentHelper(i);
				}
			}

			/// <summary>
			/// 读取玩家存储的音量设置，未存储过时返回默认值。
			/// </summary>
			/// <param name="defaultValue">配置表默认音量。</param>
			/// <returns>玩家存储的音量，无存储时为配置表默认值。</returns>
			private float LoadVolumeSetting(float defaultValue)
			{
				var storage = StorageModule.Instance;
				return storage != null ? storage.GetFloat(GetVolumeKey(), defaultValue: defaultValue) : defaultValue;
			}

			/// <summary>
			/// 读取玩家存储的静音设置，未存储过时返回默认值。
			/// </summary>
			/// <param name="defaultValue">配置表默认静音。</param>
			/// <returns>玩家存储的静音，无存储时为配置表默认值。</returns>
			private bool LoadMuteSetting(bool defaultValue)
			{
				var storage = StorageModule.Instance;
				return storage != null ? storage.GetBool(GetMuteKey(), defaultValue: defaultValue) : defaultValue;
			}

			/// <summary>
			/// 保存音量设置到本地（仅标脏，落盘由 StorageModule 自动保存与释放时 SaveAll 兜底）。
			/// </summary>
			private void SaveVolumeSetting()
			{
				var storage = StorageModule.Instance;
				if (storage == null) return; // 存储模块缺失：降级为不持久化（同 LocalizationModule 策略）
				storage.SetFloat(GetVolumeKey(), m_volume);
			}

			/// <summary>
			/// 保存静音设置到本地（仅标脏，落盘由 StorageModule 自动保存与释放时 SaveAll 兜底）。
			/// </summary>
			private void SaveMuteSetting()
			{
				var storage = StorageModule.Instance;
				if (storage == null) return; // 存储模块缺失：降级为不持久化（同 LocalizationModule 策略）
				storage.SetBool(GetMuteKey(), m_mute);
			}

			/// <summary>
			/// 获取音量设置的存储键。
			/// </summary>
			/// <returns>音量设置存储键。</returns>
			private string GetVolumeKey() => $"{STORAGE_KEY_PREFIX}_{Name}_Volume";

			/// <summary>
			/// 获取静音设置的存储键。
			/// </summary>
			/// <returns>静音设置存储键。</returns>
			private string GetMuteKey() => $"{STORAGE_KEY_PREFIX}_{Name}_Mute";

			/// <summary>
			/// 增加声音代理辅助器。
			/// </summary>
			/// <param name="idx">声音代理索引。</param>
			public void AddSoundAgentHelper(int idx)
			{
				var soundAgentGo = new GameObject($"Sound Agent - {idx}");
				soundAgentGo.transform.SetParent(transform);
				soundAgentGo.transform.localScale = Vector3.one;
				var soundAgent = soundAgentGo.GetOrAddComponent<SoundAgent>();
				soundAgent.Init(this);
				m_soundAgents.Add(soundAgent);
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

				var targetPriority = playSoundInfo.SoundParams.Priority;

				// 分两级候选，避免「同优先级替换」把已选出的严格更低优先级候选顶替掉
				//（那会变成停掉同优先级、保留更低优先级，与注释意图相反）：
				//   - lowerPriorityCandidate：严格更低优先级中优先级最低者（须扫描全部代理，不能提前 break）
				//   - samePriorityCandidate ：同优先级中 SetSoundAssetTime 最早者，仅在无任何更低优先级候选时兜底
				SoundAgent lowerPriorityCandidate = null;
				SoundAgent samePriorityCandidate  = null;

				// 遍历所有声音播放代理，找到合适的代理播放声音
				foreach (var soundAgent in m_soundAgents)
				{
					// 1.如果存在没有在播放声音的代理，则将其作为候选代理，并跳出查找。
					if (!soundAgent.IsPlaying)
					{
						candidateAgent = soundAgent;
						break;
					}

					// 2.所有的代理都在播放声音，则找到优先级较低的代理，将其设置为候选代理
					// 不移除 break：须扫描全部代理以选出优先级最低者替换，否则首个较低优先级代理会截断后续更高优先级的声音
					if (soundAgent.Priority < targetPriority)
					{
						if (lowerPriorityCandidate == null || soundAgent.Priority < lowerPriorityCandidate.Priority)
							lowerPriorityCandidate = soundAgent;
					}
					// 3.所有的代理都在播放声音，且找不到优先级较低的代理，则判断声音组中的声音是否设置了允许被同优先级声音替换，如果允许，则使用同优先级的代理作为候选代理。
					else if (AllowBeReplacedBySamePriority && soundAgent.Priority == targetPriority)
					{
						if (samePriorityCandidate == null || soundAgent.SetSoundAssetTime < samePriorityCandidate.SetSoundAssetTime)
							samePriorityCandidate = soundAgent;
					}
				}

				// 更低优先级候选优先于同优先级替换：只有完全没有更低优先级候选时才允许同优先级替换
				if (candidateAgent == null)
					candidateAgent = lowerPriorityCandidate ?? samePriorityCandidate;

				if (!candidateAgent)
				{
					playSoundInfo.SoundAssetHandle?.Release(); // 未被任何代理接管，释放句柄
					errorCode = EPlaySoundErrorCode.IgnoredBecauseLowPriority;
					return null;
				}

				if (!candidateAgent.SetSoundAsset(playSoundInfo.SoundAsset, playSoundInfo.SoundAssetHandle))
				{
					// SetSoundAsset 内部已 Reset（释放旧句柄），新句柄未被接管，需释放
					playSoundInfo.SoundAssetHandle?.Release();
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
				foreach (var soundAgent in m_soundAgents)
				{
					if (soundAgent.SerialId == serialId)
					{
						soundAgent.Stop(fadeOutSeconds);
						return true;
					}
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
				foreach (var soundAgent in m_soundAgents)
				{
					if (soundAgent.SerialId == serialId)
					{
						soundAgent.Pause(fadeOutSeconds);
						return true;
					}
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
				foreach (var soundAgent in m_soundAgents)
				{
					if (soundAgent.SerialId == serialId)
					{
						soundAgent.Resume(fadeInSeconds);
						return true;
					}
				}

				return false;
			}

			/// <summary>
			/// 停止所有已加载的声音。
			/// </summary>
			/// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
			public void StopAllLoadedSounds(float fadeOutSeconds)
			{
				foreach (var soundAgent in m_soundAgents)
				{
					if (soundAgent.IsPlaying)
						soundAgent.Stop(fadeOutSeconds);
				}
			}

			/// <summary>
			/// 重置所有声音播放代理（释放句柄，含暂停/停止状态未播放的代理）。
			/// 供模块 OnDispose 使用，避免重启时暂停中 agent 的句柄/bundle 残留。
			/// </summary>
			public void ResetAllAgents()
			{
				foreach (var soundAgent in m_soundAgents)
				{
					if (soundAgent == null) continue; // Unity teardown 时 agent 组件可能已销毁
					soundAgent.Reset();
				}
			}
		}
	}
}