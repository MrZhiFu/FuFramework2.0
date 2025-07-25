using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.UI.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using Hotfix.Manager;
using Hotfix.Network;
using Hotfix.Proto;
using Hotfix.UI.View.Main;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Login
{
    public partial class WinPlayerList : ViewBase
    {
         #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override UILayer Layer     => UILayer.Normal; // 界面所属的层级。
         protected override bool IsFullScreen => true;           // 是否是全屏界面。
         public override bool PauseCoveredUI  => false;          // 显示时是否暂停被覆盖的界面。
         //@formatter:on
         
         #endregion
        
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
        private async UniTaskVoid Login()
        {
	        // 请求玩家登录
	        var reqPlayerLogin  = new ReqPlayerLogin { Id = m_SelectedPlayerInfo.Id };
	        var respPlayerLogin = await GameApp.Network.GetNetworkChannel("network").Call<RespPlayerLogin>(reqPlayerLogin);
	        PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;

	        // 打开主界面
	        await UIManager.Instance.OpenUIAsync<WinMain>();

	        // 关闭当前界面
	        UIManager.Instance.CloseUI(this);
        }
        
        /// <summary>
        /// 网络连接成功事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNetworkConnected(object sender, GameEventArgs e)
        {
	        Login().Forget();
	        Log.Info(nameof(OnNetworkConnected));
        }
        
        /// <summary>
        /// 网络连接关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnNetworkClosed(object sender, GameEventArgs e)
        {
	        Log.Info(nameof(OnNetworkClosed));
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
			compItem.InitView(this);
			compItem.SetData(playerInfo);
		}

		private void OnBtnLoginClick(EventContext ctx)
		{
			if (networkChannel is { Connected: true })
			{
				Login().Forget();
				return;
			}

			if (networkChannel != null && GameApp.Network.HasNetworkChannel("network") && !networkChannel.Connected)
			{
				GameApp.Network.DestroyNetworkChannel("network");
			}

			networkChannel = GameApp.Network.CreateNetworkChannel("network", new DefaultNetworkChannelHelper());

			// 注册心跳消息
			var packetSendHeaderHandler = new DefaultPacketHeartBeatHandler();
			networkChannel.RegisterHeartBeatHandler(packetSendHeaderHandler);
			networkChannel.Connect(new Uri($"tcp://{serverIp}:{serverPort}"));
		}

        #endregion
    }
}