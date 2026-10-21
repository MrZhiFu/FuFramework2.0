// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 心跳保活状态：记录流逝时长与连续丢失次数，收包/重连时 Reset 清账，供断线判定。
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
		/// 重置心跳数据=>保活
		/// </summary>
		/// <param name="resetHeartBeatElapseSeconds">是否重置心跳流逝时长</param>
		public void Reset(bool resetHeartBeatElapseSeconds)
		{
			if (resetHeartBeatElapseSeconds) HeartBeatElapseSeconds = 0f;
			MissHeartBeatCount = 0;
		}
	}
}