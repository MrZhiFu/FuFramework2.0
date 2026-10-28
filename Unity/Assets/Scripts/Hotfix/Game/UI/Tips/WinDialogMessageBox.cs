using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Proto;
using System;
using FairyGUI;
using Hotfix.Framework.UI;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
	public partial class WinDialogMessageBox : WinBase
	{
		/// <summary>
		/// 对话框弹窗数据
		/// </summary>
		public sealed class DialogMessageBoxData
		{
			public string Content  { get; }
			public Action OnEnter  { get; }
			public Action OnCancel { get; }

			/// <summary>
			/// 按钮显示状态（默认全按钮；嵌套类 private 对外层 OnOpen 可见——外部经静态工厂指定，无需读取）
			/// </summary>
			public EBtnType BtnType { get; }

			/// <summary>
			/// 构造按钮对话框
			/// </summary>
			/// <param name="content">提示内容</param>
			/// <param name="onEnter">点击确定按钮的回调</param>
			/// <param name="onCancel">点击取消按钮的回调</param>
			/// <param name="btnType">按钮显示状态</param>
			public DialogMessageBoxData(string content, Action onEnter, Action onCancel, EBtnType btnType = EBtnType.ShowAll)
			{
				Content  = content;
				OnEnter  = onEnter;
				OnCancel = onCancel;
				BtnType  = btnType;
			}
		}

		private DialogMessageBoxData m_data;

		/// <summary>
		/// 初始化
		/// </summary>
		protected override void OnInit()
		{
			InitUIComp();
			InitUIEvent();
			InitEvent();
		}

		/// <summary>
		/// 界面可交互组件事件初始化
		/// </summary>
		private void InitUIEvent()
		{
			AddUIListener(btnOk.onClick, OnBtnOkClick);
			AddUIListener(btnNo.onClick, OnBtnNoClick);
		}

		/// <summary>
		/// 注册相关逻辑事件
		/// </summary>
		private void InitEvent()
		{
			// Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);
		}


		/// <summary>
		/// 界面打开
		/// </summary>
		protected override void OnOpen()
		{
			if (UserData is not DialogMessageBoxData userData) return;
			m_data          = userData;
			txtContent.text = m_data.Content;
			SetBtnTypeCtrl(m_data.BtnType);

			Refresh();
		}

		/// <summary>
		/// 界面关闭
		/// </summary>
		protected override void OnClose()
		{
			m_data = null;
		}

		/// <summary>
		/// 界面销毁
		/// </summary>
		protected override void OnDispose() { }

		/// <summary>
		/// 刷新界面
		/// </summary>
		private void Refresh()
		{
			// TODO：刷新逻辑
		}

		#region 交互事件与ListItem渲染回调处理

		private void OnBtnOkClick(EventContext ctx)
		{
			CloseSelf();
			m_data?.OnEnter?.Invoke();
		}

		private void OnBtnNoClick(EventContext ctx)
		{
			CloseSelf();
			m_data?.OnCancel?.Invoke();
		}

		#endregion
	}
}