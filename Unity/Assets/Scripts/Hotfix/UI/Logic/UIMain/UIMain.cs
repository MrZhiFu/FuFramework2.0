using GameFrameX.UI.Runtime;
using FairyGUI;
using GameFrameX.UI.FairyGUI.Runtime;
using Hotfix.Manager;

namespace Hotfix.UI
{
    public partial class UIMain
    {
        protected override void OnInit()
        {
            base.OnInit();
            OnInitUI();
        }

        protected override void OnOpen()
        {
            m_player_icon.icon  = UIPackage.GetItemURL(FUIPackage.UICommonAvatar, PlayerManager.Instance.PlayerInfo.Avatar.ToString());
            m_player_name.text  = PlayerManager.Instance.PlayerInfo.Name;
            m_player_level.text = "当前等级:" + PlayerManager.Instance.PlayerInfo.Level;
            m_bag_button.onClick.Set(OnBagBtnClick);
        }
        
        /// <summary>
        /// 点击背包按钮的回调函数
        /// </summary>
        private async void OnBagBtnClick()
        {
            // 请求背包信息
            await BagManager.Instance.RequestGetBagInfo();
            await UIManager.Instance.OpenUIAsync<UIBag>();
        }
    }
}