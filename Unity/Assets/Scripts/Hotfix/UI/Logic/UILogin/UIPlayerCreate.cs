using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Manager;
using Hotfix.Proto;

namespace Hotfix.UI
{
    public partial class UIPlayerCreate
    {
        public override void OnAwake()
        {
            UIGroup = UIManager.Instance.GetUIGroup(UILayer.Normal);
            base.OnAwake();
        }

        ReqPlayerCreate req;

        public override void OnOpen(object userData)
        {
            req = new ReqPlayerCreate();
            base.OnOpen(userData);

            var respLogin = userData as RespLogin;
            m_enter.onClick.Set(OnCreateButtonClick);
            req.Id = respLogin.Id;
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

            req.Name = m_UserName.text;

            // 创建角色
            var respPlayerCreate = await GameApp.Web.Post<RespPlayerCreate>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerCreate).ConvertToSnakeCase()}", req);
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
            var reqPlayerList  = new ReqPlayerList { Id = req.Id };
            var respPlayerList = await GameApp.Web.Post<RespPlayerList>($"http://127.0.0.1:28080/game/api/{nameof(ReqPlayerList).ConvertToSnakeCase()}", reqPlayerList);
            if (respPlayerList.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerList.ErrorCode);
                return;
            }

            // 将角色列表保存到Manager中
            AccountManager.Instance.PlayerList = respPlayerList.PlayerList;

            // 打开角色列表界面
            await UIManager.Instance.OpenUIAsync<UIPlayerList>();

            // 关闭当前界面
            UIManager.Instance.CloseUI(this);
        }
    }
}