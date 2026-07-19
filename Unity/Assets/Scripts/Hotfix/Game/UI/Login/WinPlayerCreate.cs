using Hotfix.Game.UI;
using Hotfix.Game.Tables;
using Hotfix.Game.Proto;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.UI;
using Hotfix.Game.Manager_ToDelete;
using Hotfix.Framework.Web;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class WinPlayerCreate : ViewBase
    {
         #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override EUILayer Layer         => EUILayer.Normal;   // 界面所属的层级。
         protected override EUITweenType TweenType => EUITweenType.Fade; // 界面打开/关闭时的动画效果。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
         //@formatter:on

         #endregion
         
         /// <summary>
         /// 创建角色请求
         /// </summary>
         private ReqPlayerCreate m_Req;
        
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
            // Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);
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
            m_Req = null;
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
        private async UniTaskVoid CreatePlayerAsync()
        {
            if (inputUserName.text.IsNullOrWhiteSpace())
            {
                txtError.text = "角色名不能为空";
                return;
            }

            m_Req = new ReqPlayerCreate
            {
                Id = 10000,
                Name = inputUserName.text
            };

            // 创建角色
            var respPlayerCreate =
                await WebModule.Instance.Post<RespPlayerCreate>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerCreate).ConvertToSnakeCase()}", m_Req);
            if (respPlayerCreate.ErrorCode > 0)
            {
                FuLogger.LogError("登录失败，错误信息:" + respPlayerCreate.ErrorCode);
                return;
            }

            if (respPlayerCreate.PlayerInfo != null)
                FuLogger.LogInfo("创建角色成功");

            // 获取角色列表
            var reqPlayerList = new ReqPlayerList { Id = m_Req.Id };
            var respPlayerList =
                await WebModule.Instance.Post<RespPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}",
                    reqPlayerList);
            if (respPlayerList.ErrorCode > 0)
            {
                FuLogger.LogError("登录失败，错误信息:" + respPlayerList.ErrorCode);
                return;
            }

            // 将角色列表保存到Manager中
            AccountManager.Instance.PlayerList = respPlayerList.PlayerList;

            // 关闭当前界面
            CloseSelf();

            // 打开角色列表界面
            GlobalModule.UIModule.OpenUI<WinPlayerList>();
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
            CreatePlayerAsync().Forget();
        }

        #endregion
    }
}
