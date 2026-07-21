using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
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
    public partial class WinPlayerList : ViewBase
    {
         #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override EUILayer Layer         => EUILayer.Normal;   // 界面所属的层级。
         protected override EUITweenType TweenType => EUITweenType.Fade; // 界面打开/关闭时的动画效果。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
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
            InitRedDot();
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
        /// 注册界面相关红点
        /// </summary>
        private void InitRedDot()
        {
            // Example: RedDotRegister.RegisterRedDot(this, ERedDotKey.Bag_Item, btnLogin);
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
	        await GlobalModule.UIModule.OpenUIAsync<WinMain>();

	        // 关闭当前界面
	        GlobalModule.UIModule.CloseUI(this);
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
			compItem.Init(this);
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

			// 注册心跳消息
			var packetSendHeaderHandler = new DefaultPacketHeartBeatHandler();
			networkChannel.RegisterHeartBeatHandler(packetSendHeaderHandler);
			networkChannel.Connect(new Uri($"tcp://{serverIp}:{serverPort}"));
		}

        #endregion
    }
}
