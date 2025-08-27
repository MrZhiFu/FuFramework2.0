using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Entry.Runtime;
using Hotfix.Manager;
using Hotfix.Proto;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.Login
{
    public partial class WinPlayerCreate : ViewBase
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
        /// 创建角色请求
        /// </summary>
        private ReqPlayerCreate _req;

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
        protected override void OnDispose()
        {
            _req = null;
        }

        /// <summary>
        /// 刷新界面
        /// </summary>
        private void Refresh()
        {
            // TODO：刷新逻辑
        }

        /// <summary>
        /// 创建角色按钮点击事件
        /// </summary>
        private async UniTaskVoid CreatePlayer()
        {
            if (inputUserName.text.IsNullOrWhiteSpace())
            {
                txtError.text = "角色名不能为空";
                return;
            }

            _req = new ReqPlayerCreate
            {
                Id = 10000,
                Name = inputUserName.text
            };

            // 创建角色
            var respPlayerCreate =
                await GameApp.Web.Post<RespPlayerCreate>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerCreate).ConvertToSnakeCase()}", _req);
            if (respPlayerCreate.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerCreate.ErrorCode);
                return;
            }

            if (respPlayerCreate.PlayerInfo != null)
                Log.Info("创建角色成功");

            // 获取角色列表
            var reqPlayerList = new ReqPlayerList { Id = _req.Id };
            var respPlayerList =
                await GameApp.Web.Post<RespPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}",
                    reqPlayerList);
            if (respPlayerList.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerList.ErrorCode);
                return;
            }

            // 将角色列表保存到Manager中
            AccountManager.Instance.PlayerList = respPlayerList.PlayerList;

            // 关闭当前界面
            CloseSelf();

            // 打开角色列表界面
            UIManager.Instance.OpenUI<WinPlayerList>();
        }

        #region 交互事件与ListItem渲染回调处理

        private void OnInputUserNameChanged(EventContext ctx)
        {
            // todo
        }

        private void OnInputUserNameFocusOut(EventContext ctx)
        {
            // todo
        }

        private void OnBtnCreateClick(EventContext ctx)
        {
            CreatePlayer().Forget();
        }

        #endregion
    }
}