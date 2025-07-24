using System;
using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Event.Runtime;
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using Hotfix.Manager;
using Hotfix.Network;
using Hotfix.Proto;
using UIManager = FuFramework.UI.Runtime.UIManager;

namespace Hotfix.UI
{
    public partial class UIPlayerList
    {
        private List<PlayerInfo> playerList = new();
        private PlayerInfo       m_SelectedPlayerInfo;

        private static INetworkChannel networkChannel; // 网络频道

        public static string serverIp   = "127.0.0.1"; // 服务器IP
        public static int    serverPort = 29100;       // 服务器端口

        protected override void OnInit()
        {
            base.OnInit();
            OnInitUI();

            // 订阅网络连接成功和关闭事件
            GameApp.Event.Subscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnected);
            GameApp.Event.Subscribe(NetworkClosedEventArgs.EventId,    OnNetworkClosed);
        }

        protected override void OnOpen()
        {
            playerList = AccountManager.Instance.PlayerList;

            m_login_button.onClick.Set(OnLoginButtonClick);

            m_player_list.itemRenderer = ItemRenderer;
            m_player_list.onClickItem.Set(OnPlayerListItemClick);
            // m_player_list.DataList = new List<object>(playerList);
            m_player_list.numItems = playerList.Count;
        }

        /// <summary>
        /// 登录按钮点击事件
        /// </summary>
        private void OnLoginButtonClick()
        {
            if (networkChannel is { Connected: true })
            {
                Login();
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

        /// <summary>
        /// 执行登录
        /// </summary>
        private async void Login()
        {
            // 请求玩家登录
            var reqPlayerLogin  = new ReqPlayerLogin { Id = m_SelectedPlayerInfo.Id };
            var respPlayerLogin = await GameApp.Network.GetNetworkChannel("network").Call<RespPlayerLogin>(reqPlayerLogin);
            PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;

            // 打开主界面
            await UIManager.Instance.OpenUIAsync<UIMain>();

            // 关闭当前界面
            UIManager.Instance.CloseUI(this);
        }

        /// <summary>
        /// 玩家角色列表项渲染器
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void ItemRenderer(int index, GObject item)
        {
            var playerInfo       = playerList[index];
            var uiPlayerListItem = UIPlayerListItem.GetFormPool(item as GComponent);

            uiPlayerListItem.m_level_text.text = "当前等级:" + playerInfo.Level;
            uiPlayerListItem.m_name_text.text  = playerInfo.Name;
            uiPlayerListItem.m_icon.icon       = UIPackage.GetItemURL(FUIPackage.UICommonAvatar, playerInfo.Avatar.ToString());

            item.data = playerInfo;
        }

        /// <summary>
        /// 玩家角色列表项点击事件
        /// </summary>
        /// <param name="context"></param>
        private void OnPlayerListItemClick(EventContext context)
        {
            // if ((context.data as GComponent)?.dataSource is not PlayerInfo playerInfo) return;
            //
            // m_SelectedPlayerInfo  = playerInfo;
            // m_selected_icon.icon  = UIPackage.GetItemURL(FUIPackage.UICommonAvatar, playerInfo.Avatar.ToString());
            // m_selected_name.text  = playerInfo.Name;
            // m_selected_level.text = "当前等级:" + playerInfo.Level;
            // m_IsSelected.SetSelectedIndex(1);
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

        /// <summary>
        /// 网络连接成功事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNetworkConnected(object sender, GameEventArgs e)
        {
            Login();
            Log.Info(nameof(OnNetworkConnected));
        }
    }
}