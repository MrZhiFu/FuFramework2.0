using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Proto;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.UI;
using Hotfix.Framework.Event;
using Hotfix.Framework.Network;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Scene;
using Hotfix.Game.Manager_ToDelete;
using Hotfix.Game.Network;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
	public partial class WinPlayerList : WinBase
	{
		 private List<PlayerInfo> playerList = new();
		 private PlayerInfo       m_selectedPlayerInfo;

		 public static string serverIp   = "127.0.0.1"; // 服务器IP
		 public static int    serverPort = 29100;       // 服务器端口

		/// <summary>
		/// 初始化
		/// </summary>
		protected override void OnInit()
		{
			InitUIComp();
			InitUIEvent();
		}

		/// <summary>
		/// 界面可交互组件事件初始化
		/// </summary>
		private void InitUIEvent()
		{
			AddUIListener(listPlayer.onClickItem, OnClickListPlayerItem);
			AddUIListener(btnLogin.onClick, OnBtnLoginClick);
			listPlayer.itemRenderer = OnRenderListPlayerItem;
		}

		/// <summary>
		/// 注册网络事件订阅。每次打开重订（OnClose 已退订，见下）。
		/// </summary>
		private void InitEvent()
		{
			Subscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnected);
			Subscribe(NetworkClosedEventArgs.EventId,    OnNetworkClosed);
		}


		/// <summary>
		/// 界面打开
		/// </summary>
		protected override void OnOpen()
		{
			playerList = AccountManager.Instance.PlayerList;
			listPlayer.numItems = playerList.Count;
			InitEvent();
			Refresh();
		}

		/// <summary>
		/// 界面关闭
		/// 退订网络订阅：窗口关闭（进游戏后对象池待复用）后订阅仍存活，会在重连拨通广播 Connected 时
		/// 被幽灵触发——用已关闭界面的过期数据再跑一次登录事务（双事务竞态）。
		/// </summary>
		protected override void OnClose()
		{
			UnSubscribeAll();
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

		/// <summary>
		/// 角色登录进游戏（TCP）：事务已抽入 <see cref="PlayerLoginFlow.EnterGameAsync"/>
		/// （挂阀门 → Call 建立基线 → 恢复派发，失败收尾在其中）。Token 随界面关闭自动取消。
		/// 与 WinLogin.UserLoginAsync（账号登录，HTTP）分工：账号层管"你是谁"，进游戏层管"建立会话与数据基线"。
		/// </summary>
		private async UniTaskVoid PlayerLoginAsync()
		{
			//进入游戏事务
			await PlayerLoginFlow.EnterGameAsync(m_selectedPlayerInfo.Id, Token);

			// 关闭当前界面
			GlobalModule.UIModule.Close(this);

			// 加载主场景。注意不用窗口 Token：Close(this) 已取消它，此后的链路（场景加载/开主界面）
			// 寿命比选角窗口长，归"进游戏事务"所有（PlayerLoginFlow.TransactionToken）。
			await SceneModule.Instance.LoadSceneByName("Main", PlayerLoginFlow.TransactionToken);
			
			// 打开主界面
			await GlobalModule.UIModule.OpenAsync<WinMain>();
		}

		/// <summary>
		/// 网络连接成功事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnNetworkConnected(object sender, GameEventArgs e)
		{
	        PlayerLoginAsync().Forget();
	        FuLogger.LogInfo(nameof(OnNetworkConnected));
		}

		/// <summary>
		/// 网络连接关闭事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnNetworkClosed(object sender, GameEventArgs e)
		{
	        FuLogger.LogInfo(nameof(OnNetworkClosed));
		}

		#region 交互事件与ListItem渲染回调处理

		private void OnClickListPlayerItem(EventContext ctx)
		{
			var idx = listPlayer.GetChildIndex((GObject)ctx.data);
			if (listPlayer.isVirtual) idx = listPlayer.ChildIndexToItemIndex(idx);

			m_selectedPlayerInfo  = playerList[idx];
			// var data = xxxModel:GetListDataByIdx(idx);
			loaderSelectedIcon.icon  = UIPackage.GetItemURL("Common", "wrap_1");
			txtSelectedName.text  = playerList[idx].Name;
			txtSelectedLevel.text = "当前等级:" + playerList[idx].Level;
			SetCtrlSelectedCtrl(ECtrlSelected.Yes);
		}

		private void OnRenderListPlayerItem(int idx, GObject item)
		{
			if (item is not CompPlayerListItem compItem) return;
			//var data = xxxModel:GetListPlayerDataByIdx(idx);
			var playerInfo       = playerList[idx];
			compItem.SetData(playerInfo);
		}

		private void OnBtnLoginClick(EventContext ctx)
		{
			var channel = NetworkModule.Instance.Channel;
			if (channel is { Connected: true })
			{
				PlayerLoginAsync().Forget();
				return;
			}

			if (channel != null)
			{
				// 未连接的旧频道直接销毁重建，保证装配状态干净
				NetworkModule.Instance.DestroyChannel();
			}

			channel = NetworkModule.Instance.CreateChannel();

			// 心跳处理器由框架在创建频道时自动装配（NetworkChannelHelper.Initialize →
			// 生成式静态注册表会注册游戏侧 HeartBeatProvider），此处无需再手动注册。
			// （原先的手动补注册是框架反射扫描顺序问题的兜底，已随生成式静态注册消除。）
			channel.Connect(new Uri($"tcp://{serverIp}:{serverPort}"));
		}

		#endregion
	}
}
