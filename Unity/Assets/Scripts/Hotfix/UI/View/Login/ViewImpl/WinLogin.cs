using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.UI.Runtime;
using GameFrameX.Runtime;
using Hotfix.Manager;
using Hotfix.Proto;
using UnityEngine;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Login
{
    public partial class WinLogin : ViewBase
    {
        #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override UILayer Layer         => UILayer.Normal;   // 界面所属的层级。
         protected override UITweenType TweenType => UITweenType.Fade; // 界面打开/关闭时的动画效果。
         protected override bool IsFullScreen     => true;             // 是否是全屏界面。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
        //@formatter:on

        #endregion

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
            // Example:Subscribe(XxxEventArgs.EventId, XxxEventArgs.Create(xxx));
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

        /// <summary>
        /// 执行登录
        /// </summary>
        private async UniTaskVoid Login()
        {
            if (txtUsername.text.IsNullOrWhiteSpace() || txtPassword.text.IsNullOrWhiteSpace())
            {
                txtError.text = "用户名或密码不能为空";
                return;
            }

            // 请求登录
            var req = new ReqLogin
            {
                SdkType = 0,
                SdkToken = "",
                UserName = txtUsername.text,
                Password = txtPassword.text,
                Device = SystemInfo.deviceUniqueIdentifier,
                Platform = PathHelper.GetPlatformName
            };

            var respLogin = await GameApp.Web.Post<RespLogin>($"http://127.0.0.1:28080/game/api/{nameof(ReqLogin).ConvertToSnakeCase()}", req);
            if (respLogin.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respLogin.ErrorCode);
                return;
            }

            // 获取角色列表
            var reqPlayerList = new ReqPlayerList { Id = respLogin.Id };
            var respPlayerList = await GameApp.Web.Post<RespPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}", reqPlayerList);
            if (respPlayerList.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerList.ErrorCode);
                return;
            }

            // 将角色列表保存到Manager中
            AccountManager.Instance.PlayerList = respPlayerList.PlayerList;


            if (respPlayerList.PlayerList.Count > 0)
            {
                // 有角色，打开角色列表界面
                UIManager.Instance.OpenUI<WinPlayerList>();
            }
            else
            {
                // 无角色，打开角色创建界面
                UIManager.Instance.OpenUI<WinPlayerCreate>();
            }

            // 关闭当前界面
            UIManager.Instance.CloseUI(this);
        }

    #region 交互事件与ListItem渲染回调处理

    private void OnBtnLoginClick(EventContext ctx)
    {
        Login().Forget();
    }

    private void OnInputUserNameChanged(EventContext ctx)
    {
        // todo
    }

    private void OnInputUserNameFocusOut(EventContext ctx)
    {
        // todo
    }

    private void OnInputPasswordChanged(EventContext ctx)
    {
        // todo
    }

    private void OnInputPasswordFocusOut(EventContext ctx)
    {
        // todo
    }

    #endregion
}

}