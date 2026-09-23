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
         private PlayerInfo       m_SelectedPlayerInfo;

         private static INetworkChannel networkChannel; // 网络频道

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
        /// 执行登录
        /// </summary>
        private async UniTaskVoid LoginAsync()
        {
	        // 请求玩家登录
	        var reqPlayerLogin  = new ReqPlayerLogin { Id = m_SelectedPlayerInfo.Id };
	        var respPlayerLogin = await NetworkModule.Instance.GetNetworkChannel("network").Call<RespPlayerLogin>(reqPlayerLogin);
	        PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;

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
	        LoginAsync().Forget();
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

			m_SelectedPlayerInfo  = playerList[idx];
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
			if (networkChannel is { Connected: true })
			{
				LoginAsync().Forget();
				return;
			}

			if (networkChannel != null && NetworkModule.Instance.HasNetworkChannel("network") && !networkChannel.Connected)
			{
				NetworkModule.Instance.DestroyNetworkChannel("network");
			}

			networkChannel = NetworkModule.Instance.CreateNetworkChannel("network", new DefaultNetworkChannelHelper());

			// 心跳处理器由框架在创建频道时自动装配（DefaultNetworkChannelHelper.Initialize →
			// 生成式静态注册表会注册游戏侧 DefaultPacketHeartBeatHandler），此处无需再手动注册。
			// （原先的手动补注册是框架反射扫描顺序问题的兜底，已随生成式静态注册消除。）
			networkChannel.Connect(new Uri($"tcp://{serverIp}:{serverPort}"));
		}

        #endregion
    }
}
