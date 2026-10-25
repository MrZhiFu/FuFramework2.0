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
			InitEvent();
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
		/// 注册相关逻辑事件
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

		/// <summary>
		/// 角色登录进游戏（TCP）：挂起推送派发 → Call(ReqPlayerLogin) 建立全量基线 → 恢复派发 → 打开主界面。
		/// 与 WinLogin.UserLoginAsync（账号登录，HTTP）分工：账号层管"你是谁"，进游戏层管"建立会话与数据基线"。
		/// </summary>
		private async UniTaskVoid PlayerLoginAsync()
		{
			// 挂起推送派发：登录窗口期的增量推送进缓冲（全量基线未建立前不可派发）
			NetworkModule.Instance.SuspendNotifyDispatch();
			try
			{
				// 请求玩家登录
				var reqPlayerLogin  = new ReqPlayerLogin { Id = m_selectedPlayerInfo.Id };
				var respPlayerLogin = await NetworkModule.Instance.Call(reqPlayerLogin);
				PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;

				// 基线就绪：恢复派发，缓冲推送按到达顺序回放
				NetworkModule.Instance.ResumeNotifyDispatch();
			}
			catch (Exception)
			{
				// 登录失败：基线未建立，缓冲中的增量全是脏数据，直接丢弃不回放
				NetworkModule.Instance.ClearNotifyBuffer();
				throw;
			}

			// 打开主界面
			await GlobalModule.UIModule.OpenAsync<WinMain>();

			// 关闭当前界面
			GlobalModule.UIModule.Close(this);
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
			SetController(ECtrlSelected.Yes);
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

			channel = NetworkModule.Instance.CreateChannel(new NetworkChannelHelper());

			// 心跳处理器由框架在创建频道时自动装配（NetworkChannelHelper.Initialize →
			// 生成式静态注册表会注册游戏侧 PacketHeartBeatHandler），此处无需再手动注册。
			// （原先的手动补注册是框架反射扫描顺序问题的兜底，已随生成式静态注册消除。）
			channel.Connect(new Uri($"tcp://{serverIp}:{serverPort}"));
		}

		#endregion
	}
}
