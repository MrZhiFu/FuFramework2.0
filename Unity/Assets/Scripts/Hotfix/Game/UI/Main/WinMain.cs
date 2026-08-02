using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
using Hotfix.Framework.UI;
using Hotfix.Game.Manager_ToDelete;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class WinMain : WinBase
    {
        /// <summary>
        /// 初始化
        /// </summary>  
        protected override void OnInit()
        {
            InitUIComp();
            InitUIEvent();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void RegisterEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);
        }


        /// <summary>
        /// 界面打开
        /// </summary>
        protected override void OnOpen()
        {
            RegisterEvent();
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
            loaderPlayerIcon.icon = UIPackage.GetItemURL("Common", "wrap_1"); //PlayerManager.Instance.PlayerInfo.Avatar.ToString());
            txtPlayerName.text    = PlayerManager.Instance.PlayerInfo.Name;
            txtPlayerLevel.text   = "当前等级:" + PlayerManager.Instance.PlayerInfo.Level;
        }

        private async UniTaskVoid ReqBagInfoAsync()
        {
            // 请求背包信息
            await BagManager.Instance.RequestGetBagInfoAsync();
            await GlobalModule.UIModule.OpenAsync<WinBag>();
        }

        #region 交互事件与ListItem渲染回调处理

        private void OnBtnBagClick(EventContext ctx)
        {
            ReqBagInfoAsync().Forget();
        }

        #endregion
    }
}
