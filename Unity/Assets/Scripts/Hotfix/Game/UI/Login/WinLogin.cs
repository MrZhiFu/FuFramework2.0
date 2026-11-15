using System;
using System.Threading;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Proto;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
using Hotfix.Framework.Localization;
using AOT.Framework.Core.Utility;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Framework.UI;
using Hotfix.Framework.Sound;
using Hotfix.Framework.Web;
using Hotfix.Game.Manager_ToDelete;
using UnityEngine;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
	public partial class WinLogin : WinBase
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
			AddUIListener(btnLogin.onClick, OnBtnLoginClick);
			AddUIListener(inputUserName.onChanged, OnInputUserNameChanged);
			AddUIListener(inputUserName.onFocusOut, OnInputUserNameFocusOut);
			AddUIListener(inputPassword.onChanged, OnInputPasswordChanged);
			AddUIListener(inputPassword.onFocusOut, OnInputPasswordFocusOut);
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

		/// <summary>
		/// 登录按钮点击：账号登录（HTTP）：验证凭证 → 获取角色列表 → 跳转角色界面。
		/// 异步签名直接挂 AddUIListener：生命周期取消（窗口关闭时在途请求自动取消）与异常
		/// 由 UI 事件适配层统一处理，业务内无需 try/catch、无需 .Forget() 包装方法。
		/// 与 WinPlayerList.PlayerLoginAsync（角色登录进游戏，TCP）分工：账号层管"你是谁"，进游戏层管"建立会话与数据基线"。
		/// </summary>
		private async UniTask OnBtnLoginClick(EventContext ctx)
		{
			if (txtUsername.text.IsNullOrWhiteSpace() || txtPassword.text.IsNullOrWhiteSpace())
			{
				txtError.text = "用户名或密码不能为空";
				return;
			}

			// 请求登录
			var req = new ReqUserLogin
			{
				SdkType  = 0,
				SdkToken = "",
				UserName = txtUsername.text,
				Password = txtPassword.text,
				Device   = SystemInfo.deviceUniqueIdentifier,
				Platform = UtilityAOT.Application.PlatformName
			};

			var rspLogin = await WebModule.Instance.Post<RspUserLogin>($"http://127.0.0.1:28080/game/api/{nameof(ReqUserLogin).ConvertToSnakeCase()}", req, Token);
			if (rspLogin.ErrorCode > 0)
			{
				FuLogger.LogError("登录失败，错误信息:" + rspLogin.ErrorCode);
				return;
			}

			// 获取角色列表
			var reqPlayerList  = new ReqPlayerList { Id = rspLogin.Id };
			var rspPlayerList = await WebModule.Instance.Post<RspPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}", reqPlayerList, Token);
			if (rspPlayerList.ErrorCode > 0)
			{
				FuLogger.LogError("登录失败，错误信息:" + rspPlayerList.ErrorCode);
				return;
			}

			// 将角色列表保存到Manager中
			AccountManager.Instance.PlayerList = rspPlayerList.PlayerList;

			if (rspPlayerList.PlayerList.Count > 0)
				GlobalModule.UIModule.Open<WinPlayerList>(); // 有角色，打开角色列表界面
			else
				GlobalModule.UIModule.Open<WinPlayerCreate>(); // 无角色，打开角色创建界面

			// 关闭当前界面
			CloseSelf();
		}

		private void OnInputUserNameChanged(EventContext ctx)
		{
			// todo
		}

		private void OnInputUserNameFocusOut(EventContext ctx)
		{
			// if (_soundId != -1)
			// SoundModule.Instance.PauseSound(_soundId);
		}

		private void OnInputPasswordChanged(EventContext ctx)
		{
			// todo
		}

		private void OnInputPasswordFocusOut(EventContext ctx)
		{
			// if (_soundId != -1)
			// SoundModule.Instance.ResumeSound(_soundId);
		}

		// Example: RedDotModule.Instance.RegisterLeaf(ERedDotKey.Bag_Item, () => GetBagRedDotCount(), "BagChanged");

		#endregion
	}
}
