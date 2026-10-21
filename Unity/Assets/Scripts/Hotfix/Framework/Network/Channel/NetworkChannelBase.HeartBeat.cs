using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——心跳分部：心跳字段、保活调度与断线判定。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// 默认心跳间隔
		/// </summary>
		private const float DEFAULT_HEART_BEAT_INTERVAL = 30f;

		/// <summary>
		/// 默认心跳丢失断开次数
		/// </summary>
		private const int DEFAULT_MISS_HEART_BEAT_COUNT_BY_CLOSE = 10;

		/// <summary>
		/// 心跳间隔
		/// </summary>
		protected float m_HeartBeatInterval;

		/// <summary>
		/// 心跳丢失次数
		/// </summary>
		protected int m_MissHeartBeatCountByClose;

		/// <summary>
		/// 心跳状态
		/// </summary>
		protected readonly HeartBeatState m_HeartBeatState;

		/// <summary>
		/// 心跳状态的专用锁对象。
		/// 说明：原先直接 lock(m_HeartBeatState) 作为锁使用，而 Close() 在 lock(this) 内再次获取该锁、
		/// ProcessHeartBeat 又在持有该锁时调用 Close()，两条路径锁序相反会形成 ABBA 死锁面，
		/// 因此统一改为专用锁对象，并保证不在持有该锁时再获取关闭锁。
		/// </summary>
		protected readonly object m_HeartBeatLock = new();

		/// <summary>
		/// 处理心跳
		/// </summary>
		/// <param name="unscaledDeltaTime"></param>
		private void ProcessHeartBeat(float unscaledDeltaTime)
		{
			if (m_HeartBeatInterval <= 0f) return;

			var sendHeartBeat      = false;
			var missHeartBeatCount = 0;
			lock (m_HeartBeatLock)
			{
				if (m_Socket == null || !PActive) return;

				m_HeartBeatState.HeartBeatElapseSeconds += unscaledDeltaTime;
				if (m_HeartBeatState.HeartBeatElapseSeconds >= m_HeartBeatInterval)
				{
					sendHeartBeat      = true;
					missHeartBeatCount = m_HeartBeatState.MissHeartBeatCount;

					m_HeartBeatState.HeartBeatElapseSeconds = 0f;
					m_HeartBeatState.MissHeartBeatCount++;
				}
			}

			// 以下操作必须在心跳锁之外执行：SendHeartBeat 会获取发送包池锁，
			// Close 会获取关闭锁，若仍在心跳锁内执行会与 Close 内的加锁形成 ABBA 死锁。
			if (sendHeartBeat && m_NetworkChannelHelper.SendHeartBeat())
			{
				if (missHeartBeatCount > 0 && m_NetworkChannelMissHeartBeat != null)
				{
					m_NetworkChannelMissHeartBeat(this, missHeartBeatCount);
				}

				return;
			}

			bool shouldClose;
			lock (m_HeartBeatLock)
			{
				shouldClose = m_HeartBeatState.MissHeartBeatCount > m_MissHeartBeatCountByClose;
			}

			if (shouldClose)
			{
				// 心跳丢失达到上限。触发断开
				Close();
			}
		}
	}
}