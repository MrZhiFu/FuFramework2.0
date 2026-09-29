using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using AOT.Launch.Localization;
using AOT.Launch.UI;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace AOT.Launch
{
	/// <summary>
	/// AOT 启动加载界面。
	///     功能：显示进度、提示文本、更新确认框，脱离 UIModule/EventModule 自包含运行。
	///     UI 组件绑定部分见 WinLauncher.Gen.cs。
	/// </summary>
	public sealed class LaunchView : ILaunchView
	{
		/// <summary>
		/// 资源加载UI界面。
		/// </summary>
		private WinLauncher m_winLauncher;

		/// <summary>
		/// 更新确认框的回调。
		/// </summary>
		private Action m_onConfirm;

		/// <summary>
		/// 创建并显示加载界面。
		/// </summary>
		public static UniTask<LaunchView> CreateAsync()
		{
			var view = new LaunchView();
			view.Init();
			return UniTask.FromResult(view);
		}

		/// <summary>
		/// 初始化加载界面。
		/// </summary>
		private void Init()
		{
			LoadUIPackage();
			InitWinLauncher();
			SetNeedUpgrade(false);
			SetDownloading(false);
		}

		/// <summary>
		/// 从 Resources 加载 Launcher FUI 包并创建 UI 组件（启动阶段不依赖 YooAsset）。
		/// </summary>
		private void LoadUIPackage()
		{
			UIPackage.AddPackage("UI/Launcher");

			m_winLauncher = new WinLauncher
			{
				m_view = UIPackage.CreateObject("Launcher", "WinLauncher").asCom
			};
			m_winLauncher.m_view.MakeFullScreen();

			GRoot.inst.AddChild(m_winLauncher.m_view);
		}

		/// <summary>
		/// 初始化资源加载UI界面。
		/// </summary>
		private void InitWinLauncher()
		{
			m_winLauncher.InitUIComp();
			m_winLauncher.btnOk.onClick.Set(OnBtnOkClick);
		}

		/// <summary>
		/// 设置提示文本。
		/// </summary>
		/// <param name="text">提示文本</param>
		public void SetTip(string text)
		{
			if (m_winLauncher.txtTips != null)
				m_winLauncher.txtTips.text = text;
		}

		/// <summary>
		/// 设置下载进度与提示。
		/// </summary>
		/// <param name="progress">下载进度(0~1)</param>
		/// <param name="text">提示文本</param>
		public void SetProgress(float progress, string text)
		{
			SetDownloading(true);

			if (m_winLauncher.progressBar != null)
				m_winLauncher.progressBar.value = progress * 100f;

			SetTip(text);
		}

		/// <summary>
		/// 设置是否显示更新确认框。
		/// </summary>
		/// <param name="need">是否显示更新确认框</param>
		public void SetNeedUpgrade(bool need) => m_winLauncher.SetController(need ? WinLauncher.EIsNeedUpgrade.Yes : WinLauncher.EIsNeedUpgrade.No);

		/// <summary>
		/// 设置是否处于下载中状态。
		/// </summary>
		/// <param name="downloading">是否处于下载中状态</param>
		public void SetDownloading(bool downloading) => m_winLauncher.SetController(downloading ? WinLauncher.EIsDownloading.Yes : WinLauncher.EIsDownloading.No);

		/// <summary>
		/// 显示更新确认框。
		/// </summary>
		/// <param name="content">更新内容</param>
		/// <param name="onConfirm">确认回调</param>
		public void ShowUpdateDialog(string content, Action onConfirm)
		{
			SetNeedUpgrade(true);
			m_winLauncher.btnOk.title     = LaunchLocalization.GetLanguage(LaunchL10nKey.aot_update_dialog_ok_btn);
			m_winLauncher.txtContent.text = content;
			m_winLauncher.txtContent.onClick.Set(ctx =>
			{
				if (ctx.data != null) UtilityAOT.Application.OpenURL(ctx.data.ToString());
			});
			m_onConfirm = onConfirm;
		}

		/// <summary>
		/// 确认按钮点击事件处理。
		/// </summary>
		/// <param name="ctx"></param>
		private void OnBtnOkClick(EventContext ctx) => m_onConfirm?.Invoke();

		/// <summary>
		/// 关闭并销毁加载界面。
		/// </summary>
		public void Close()
		{
			if (m_winLauncher?.m_view == null) return;
			GRoot.inst.RemoveChild(m_winLauncher.m_view, true);
			m_winLauncher = null;
		}
	}
}