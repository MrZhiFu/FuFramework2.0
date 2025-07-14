using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Manager;
using Hotfix.Proto;
using UnityEngine;

namespace Hotfix.UI
{
    public partial class UILogin
    {
        protected override void OnInit()
        {
            Layer = UILayer.Normal;
            base.OnInit();
            OnInitUI();
        }

        protected override void OnOpen()
        {
            m_enter.onClick.Set(OnLoginClick);
        }

        private void OnLoginClick()
        {
            Login();
        }

        /// <summary>
        /// 执行登录
        /// </summary>
        private async void Login()
        {
            if (m_UserName.text.IsNullOrWhiteSpace() || m_Password.text.IsNullOrWhiteSpace())
            {
                m_ErrorText.text = "用户名或密码不能为空";
                return;
            }

            // 请求登录
            var req = new ReqLogin
            {
                SdkType  = 0,
                SdkToken = "",
                UserName = m_UserName.text,
                Password = m_Password.text,
                Device   = SystemInfo.deviceUniqueIdentifier,
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
                UIManager.Instance.OpenUIAsync<UIPlayerList>();
            }
            else
            {
                // 无角色，打开角色创建界面
                UIManager.Instance.OpenUIAsync<UIPlayerCreate>();
            }

            // 关闭当前界面
            UIManager.Instance.CloseUI(this);
        }
    }
}