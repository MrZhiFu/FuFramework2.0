using GameFrameX.Runtime;
using Hotfix.Manager;
using Hotfix.Proto;
using UIManager = FuFramework.UI.Runtime.UIManager;

namespace Hotfix.UI
{
    public partial class UIPlayerCreate
    {
        protected override void OnInit()
        {
            base.OnInit();
            OnInitUI();
        }

        ReqPlayerCreate _req;

        protected override void OnOpen()
        {
            _req = new ReqPlayerCreate();
            var respLogin = UserData as RespLogin;
            m_enter.onClick.Set(OnCreateButtonClick);
            if (respLogin != null) 
                _req.Id = respLogin.Id;
        }

        /// <summary>
        /// 创建角色按钮点击事件
        /// </summary>
        private async void OnCreateButtonClick()
        {
            if (m_UserName.text.IsNullOrWhiteSpace())
            {
                m_ErrorText.text = "角色名不能为空";
                return;
            }

            _req.Name = m_UserName.text;

            // 创建角色
            var respPlayerCreate = await GameApp.Web.Post<RespPlayerCreate>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerCreate).ConvertToSnakeCase()}", _req);
            if (respPlayerCreate.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerCreate.ErrorCode);
                return;
            }

            if (respPlayerCreate.PlayerInfo != null)
            {
                Log.Info("创建角色成功");
            }

            // 获取角色列表
            var reqPlayerList  = new ReqPlayerList { Id = _req.Id };
            var respPlayerList = await GameApp.Web.Post<RespPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}", reqPlayerList);
            if (respPlayerList.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerList.ErrorCode);
                return;
            }

            // 将角色列表保存到Manager中
            AccountManager.Instance.PlayerList = respPlayerList.PlayerList;

            // 打开角色列表界面
            UIManager.Instance.OpenUI<UIPlayerList>();

            // 关闭当前界面
            UIManager.Instance.CloseUI(this);
        }
    }
}