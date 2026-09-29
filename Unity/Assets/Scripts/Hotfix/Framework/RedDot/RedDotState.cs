using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
	/// <summary>
	/// 红点节点状态
	/// </summary>
	public struct RedDotState
	{
		/// <summary>
		/// 红点数量（TotalCount）
		/// </summary>
		public int m_Count;

		/// <summary>
		/// 节点是否激活
		/// </summary>
		public bool m_IsActive;

		/// <summary>
		/// 显示模式
		/// </summary>
		public ERedDotDisplayMode m_DisplayMode;

		/// <summary>
		/// 静态空状态
		/// </summary>
		public static readonly RedDotState sr_Empty = new()
		{
			m_Count       = 0,
			m_IsActive    = false,
			m_DisplayMode = ERedDotDisplayMode.DotOnly
		};
	}
}