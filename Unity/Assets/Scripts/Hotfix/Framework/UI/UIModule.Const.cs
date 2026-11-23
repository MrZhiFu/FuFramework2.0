// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
	/// <summary>
	/// 界面管理模块——常量分部：窗口池与背景模糊运行时参数（单一事实源）。
	/// </summary>
	public sealed partial class UIModule
	{
		#region 窗口对象池默认参数

		/// <summary>
		/// 界面实例对象池自动销毁检查的间隔秒数。
		/// </summary>
		private const float DEFAULT_AUTO_DISPOSE_CHECK_INTERVAL = 60f;

		/// <summary>
		/// 界面实例对象池的容量。
		/// </summary>
		private const int DEFAULT_POOL_CAPACITY = 16;

		/// <summary>
		/// 界面实例对象池对象过期秒数。
		/// </summary>
		private const float DEFAULT_POOL_EXPIRE_TIME_AFTER_IDLE = 60f;

		#endregion

		#region 背景模糊

		/// <summary>
		/// 模糊 Shader 资源名称。
		/// </summary>
		private const string BLUR_SHADER_NAME = "UIBlurBackground";

		/// <summary>
		/// 模糊采样步长（BRP 半分辨率纹素翻倍 → URP 0.8 × 2）。
		/// </summary>
		private const float BLUR_SIZE = 1.6f;

		/// <summary>
		/// 压暗强度（0=不压暗，1=全黑）。参考文档 Blur 类型：强模糊 + 强压暗。
		/// </summary>
		private const float MASK_POWER = 0.35f;

		#endregion
	}
}
