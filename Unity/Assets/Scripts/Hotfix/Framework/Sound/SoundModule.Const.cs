// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Sound
{
	/// <summary>
	/// 声音管理模块——常量分部：声音运行时参数（单一事实源）。
	/// </summary>
	public sealed partial class SoundModule
	{
		#region 资源路径

		/// <summary>
		/// AudioMixer 资源路径：Bundle 内混响器资源，OnInit 异步加载，加载失败视为致命错误。
		/// </summary>
		private const string AUDIO_MIXER_ASSET_PATH = "Assets/Bundles/Sound/_MainAudioMixer.mixer";

		#endregion

		#region 声音参数

		/// <summary>
		/// 声音最大可听距离默认值（Unity 世界单位）：SoundParams 默认构造/Clear 与 SoundAgent.Reset 的统一复位值。
		/// </summary>
		internal const float SOUND_MAX_DISTANCE = 100f;

		#endregion
	}
}
