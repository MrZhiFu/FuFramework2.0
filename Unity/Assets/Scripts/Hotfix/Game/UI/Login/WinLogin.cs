using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
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

        private void OnBtnLoginClick(EventContext ctx)
        {
            LoginAsync().Forget();
            // PlayBgm().Forget();
            // Example: RedDotModule.Instance.RegisterLeaf(ERedDotKey.Bag_Item, () => GetBagRedDotCount(), "BagChanged");
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

        #endregion

        /// <summary>
        /// 执行登录
        /// </summary>
        private async UniTaskVoid LoginAsync()
        {
            if (txtUsername.text.IsNullOrWhiteSpace() || txtPassword.text.IsNullOrWhiteSpace())
            {
                txtError.text = "用户名或密码不能为空";
                return;
            }

            // 请求登录
            var req = new ReqLogin
            {
                SdkType  = 0,
                SdkToken = "",
                UserName = txtUsername.text,
                Password = txtPassword.text,
                Device   = SystemInfo.deviceUniqueIdentifier,
                Platform = UtilityAOT.Application.PlatformName
            };

            var respLogin = await WebModule.Instance.Post<RespLogin>($"http://127.0.0.1:28080/game/api/{nameof(ReqLogin).ConvertToSnakeCase()}", req);
            if (respLogin.ErrorCode > 0)
            {
                FuLogger.LogError("登录失败，错误信息:" + respLogin.ErrorCode);
                return;
            }

            // 获取角色列表
            var reqPlayerList  = new ReqPlayerList { Id = respLogin.Id };
            var respPlayerList = await WebModule.Instance.Post<RespPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}", reqPlayerList);
            if (respPlayerList.ErrorCode > 0)
            {
                FuLogger.LogError("登录失败，错误信息:" + respPlayerList.ErrorCode);
                return;
            }

            // 将角色列表保存到Manager中
            AccountManager.Instance.PlayerList = respPlayerList.PlayerList;

            if (respPlayerList.PlayerList.Count > 0)
                GlobalModule.UIModule.Open<WinPlayerList>(); // 有角色，打开角色列表界面
            else
                GlobalModule.UIModule.Open<WinPlayerCreate>(); // 无角色，打开角色创建界面

            // 关闭当前界面
            CloseSelf();
        }

        // public async UniTaskVoid PlayBgm()
        // {
        //     _soundId = await SoundModule.Instance.PlaySound("sfx_lose", "UI", ".ogg");
        // }
    }
}
