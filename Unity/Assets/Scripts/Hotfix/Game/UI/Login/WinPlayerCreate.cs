using System;
using System.Threading;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Proto;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.UI;
using Hotfix.Game.Manager_ToDelete;
using Hotfix.Framework.Web;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
	public partial class WinPlayerCreate : WinBase
	{
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
			AddUIListener(inputUserName.onChanged, OnInputUserNameChanged);
			AddUIListener(inputUserName.onFocusOut, OnInputUserNameFocusOut);
			AddUIListener(btnCreate.onClick, OnBtnCreateClick);
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
			Refresh();
		}

		/// <summary>
		/// 界面关闭
		/// </summary>
		protected override void OnClose() { }

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

		private void OnInputUserNameChanged(EventContext ctx)
		{
			// todo
		}

		private void OnInputUserNameFocusOut(EventContext ctx)
		{
			// todo
		}

		/// <summary>
		/// 创建角色按钮点击：校验 → 创建角色 → 刷新角色列表 → 关闭本窗并打开角色列表。
		/// 生命周期取消与异常由 UI 事件适配层统一处理（窗口关闭时在途请求自动取消，业务内无需 try/catch）。
		/// </summary>
		private async UniTask OnBtnCreateClick(EventContext ctx)
		{
			if (inputUserName.text.IsNullOrWhiteSpace())
			{
				txtError.text = "角色名不能为空";
				return;
			}

			var req = new ReqPlayerCreate
			{
				Id = 10000,
				Name = inputUserName.text
			};

			// 创建角色
			var rspPlayerCreate =
				await WebModule.Instance.Post<RspPlayerCreate>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerCreate).ConvertToSnakeCase()}", req, Token);
			if (rspPlayerCreate.ErrorCode > 0)
			{
				FuLogger.LogError("登录失败，错误信息:" + rspPlayerCreate.ErrorCode);
				return;
			}

			if (rspPlayerCreate.PlayerInfo != null)
				FuLogger.LogInfo("创建角色成功");

			// 获取角色列表
			var reqPlayerList = new ReqPlayerList { Id = req.Id };
			var rspPlayerList =
				await WebModule.Instance.Post<RspPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}",
					reqPlayerList, Token);
			if (rspPlayerList.ErrorCode > 0)
			{
				FuLogger.LogError("登录失败，错误信息:" + rspPlayerList.ErrorCode);
				return;
			}

			// 将角色列表保存到Manager中
			AccountManager.Instance.PlayerList = rspPlayerList.PlayerList;

			// 关闭当前界面
			CloseSelf();

			// 打开角色列表界面
			GlobalModule.UIModule.Open<WinPlayerList>();
		}

		#endregion
	}
}
