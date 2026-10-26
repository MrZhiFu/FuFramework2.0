using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 心跳保活状态：记录流逝时长与连续丢失次数，收包/重连时 Reset 清账，供断线判定。
	/// 流逝时长的推进采用<b>真实时钟</b>（<see cref="Time.realtimeSinceStartup"/> 差值）而非逐帧累加：
	/// 挂后台时 Unity 停止调用 Update、帧累加随之停走，改用墙钟差值后，回前台首帧的差值即挂起时长——
	/// 立即"到点"补发心跳体检连接（活连接正常回包清账，僵尸连接进丢失计数判死），
	/// 不必再等下一个心跳间隔（修正"切微信回来点啥都没反应、半分钟才弹重连"的旧短板）。
	/// </summary>
	internal sealed class HeartBeatState
	{
		/// <summary>
		/// 心跳丢失次数
		/// </summary>
		public int MissHeartBeatCount { get; set; }

		/// <summary>
		/// 心跳间隔时长
		/// </summary>
		public float HeartBeatElapseSeconds { get; set; }

		/// <summary>
		/// 真实时钟基点：最后一次心跳相关事件（逐帧推进/收包清账/连接重置）发生时的
		/// <see cref="Time.realtimeSinceStartup"/>。下一次推进以它为起点计算真实间隔。
		/// </summary>
		private float m_lastRealTimeSinceStartup;

		/// <summary>
		/// 用真实时钟差值推进心跳流逝时长（替代逐帧累加 unscaledDeltaTime）。
		/// 由主线程 ProcessHeartBeat 每帧调用；挂起期间无调用，回前台首帧的差值即整段挂起时长。
		/// </summary>
		public void UpdateByRealTime()
		{
			var now = Time.realtimeSinceStartup;
			HeartBeatElapseSeconds     += now - m_lastRealTimeSinceStartup;
			m_lastRealTimeSinceStartup =  now;
		}

		/// <summary>
		/// 重置心跳数据=>保活
		/// </summary>
		/// <param name="resetHeartBeatElapseSeconds">是否重置心跳流逝时长</param>
		/// <remarks>
		/// 无论哪种重置都会刷新真实时钟基点：收包清账/连接重置意味着"刚发生过心跳相关事件"，
		/// 下一次推进从当前真实时刻重新分段累计（分段累计之和不变，不影响"每 N 秒必发"的节奏）。
		/// 本方法会在后台收包线程被调用（调用点均持 <c>m_HeartBeatLock</c>），与主线程推进互斥，线程安全同现状。
		/// </remarks>
		public void Reset(bool resetHeartBeatElapseSeconds)
		{
			if (resetHeartBeatElapseSeconds) HeartBeatElapseSeconds = 0f;
			m_lastRealTimeSinceStartup = Time.realtimeSinceStartup;
			MissHeartBeatCount         = 0;
		}
	}
}