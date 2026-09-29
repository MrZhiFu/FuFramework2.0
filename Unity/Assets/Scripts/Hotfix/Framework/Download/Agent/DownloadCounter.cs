using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载计数器。
	/// 功能：
	///     1. 记录下载进度。
	///     2. 计算下载速度。
	///     3. 记录下载时间。
	/// </summary>
	internal sealed class DownloadCounter
	{
		/// 下载计数器链表容器
		private readonly FuLinkedList<DownloadCounterNode> m_downloadCounterNodeList;

		/// 更新间隔(秒)
		private float m_updateInterval;

		/// 记录间隔(秒)
		private float m_recordInterval;

		/// 计数累加器
		private float m_accumulator;

		/// 剩余时间(秒)
		private float m_leftTime;

		/// 当前下载进度
		public float CurrentSpeed { get; private set; }

		/// 更新间隔(秒)
		// ReSharper disable once UnusedMember.Local
		public float UpdateInterval
		{
			get => m_updateInterval;
			set
			{
				if (value <= 0f) throw new InvalidOperationException("更新间隔无效，必须大于0.");
				m_updateInterval = value;
				Reset();
			}
		}

		/// 记录间隔(秒)
		// ReSharper disable once UnusedMember.Local
		public float RecordInterval
		{
			get => m_recordInterval;
			set
			{
				if (value <= 0f) throw new InvalidOperationException("记录间隔无效，必须大于0.");
				m_recordInterval = value;
				Reset();
			}
		}

		/// <summary>
		/// 构造一个下载计数器
		/// </summary>
		/// <param name="updateInterval">更新间隔(秒，默认为1秒1次)</param>
		/// <param name="recordInterval">记录间隔(秒，默认为10秒1次)</param>
		public DownloadCounter(float updateInterval, float recordInterval)
		{
			if (updateInterval <= 0f) throw new InvalidOperationException("更新间隔无效，必须大于0.");
			if (recordInterval <= 0f) throw new InvalidOperationException("记录间隔无效，必须大于0.");

			m_downloadCounterNodeList = new FuLinkedList<DownloadCounterNode>();

			m_updateInterval = updateInterval;
			m_recordInterval = recordInterval;

			Reset();
		}

		/// <summary>
		/// 关闭清理
		/// </summary>
		public void Shutdown() => Reset();

		/// <summary>
		/// 下载计数器轮询
		/// </summary>
		/// <param name="deltaTime">逻辑帧间隔流逝时间，以秒为单位。</param>
		/// <param name="unscaledDeltaTime">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
		public void Update(float deltaTime, float unscaledDeltaTime)
		{
			if (m_downloadCounterNodeList.Count <= 0) return;

			m_accumulator += unscaledDeltaTime;
			if (m_accumulator > m_recordInterval)
				m_accumulator = m_recordInterval;

			m_leftTime -= unscaledDeltaTime;
			foreach (var downloadCounterNode in m_downloadCounterNodeList)
			{
				downloadCounterNode.Update(deltaTime, unscaledDeltaTime);
			}

			while (m_downloadCounterNodeList.Count > 0)
			{
				var downloadCounterNode = m_downloadCounterNodeList.First.Value;
				if (downloadCounterNode.ElapseSeconds < m_recordInterval) break;

				ReferencePool.Recycle(downloadCounterNode);
				m_downloadCounterNodeList.RemoveFirst();
			}

			if (m_downloadCounterNodeList.Count <= 0)
			{
				Reset();
				return;
			}

			if (m_leftTime <= 0f)
			{
				var totalDeltaLength = 0L;
				foreach (var downloadCounterNode in m_downloadCounterNodeList)
				{
					totalDeltaLength += downloadCounterNode.DeltaLength;
				}

				CurrentSpeed =  m_accumulator > 0f ? totalDeltaLength / m_accumulator : 0f;
				m_leftTime   += m_updateInterval;
			}
		}

		/// <summary>
		/// 记录差值大小
		/// </summary>
		/// <param name="deltaLength">差值大小</param>
		public void RecordDeltaLength(int deltaLength)
		{
			if (deltaLength <= 0) return;

			DownloadCounterNode downloadCounterNode;
			if (m_downloadCounterNodeList.Count > 0)
			{
				downloadCounterNode = m_downloadCounterNodeList.Last.Value;
				if (downloadCounterNode.ElapseSeconds < m_updateInterval)
				{
					downloadCounterNode.AddDeltaLength(deltaLength);
					return;
				}
			}

			downloadCounterNode = DownloadCounterNode.Create();
			downloadCounterNode.AddDeltaLength(deltaLength);
			m_downloadCounterNodeList.AddLast(downloadCounterNode);
		}

		private void Reset()
		{
			foreach (var downloadCounterNode in m_downloadCounterNodeList)
			{
				ReferencePool.Recycle(downloadCounterNode);
			}

			m_downloadCounterNodeList.Clear();
			CurrentSpeed  = 0f;
			m_accumulator = 0f;
			m_leftTime    = 0f;
		}
	}
}