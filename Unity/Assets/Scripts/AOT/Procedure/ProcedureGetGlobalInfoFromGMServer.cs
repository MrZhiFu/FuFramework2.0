using System;
using Cysharp.Threading.Tasks;
using FuFramework.Asset.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Web.Runtime;
using YooAsset;
using Utility = FuFramework.Core.Runtime.Utility;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 获取后台服务端全局信息流程。
    /// 主要作用是：
    /// 1. 获取全局信息，包括：服务器地址、资源版本地址、内容信息
    /// 2. 获取成功，保存全局信息到globalConfigComponent组件中，并进入获取App版本号流程
    /// 3. 若获取失败，则提示网络异常，并延迟3秒后重试。。 
    ///  </summary>
    public class ProcedureGetGlobalInfoFromGmServer : ProcedureBase
    {
        /// <summary>
        /// 全局信息的服务器地址
        /// </summary>
        private const string GlobalInfoUrl = "http://127.0.0.1:20808/api/GameGlobalInfo/GetInfo";

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 编辑器下的模拟模式--直接进入获取App版本号流程
            if (AssetManager.Instance.PlayMode == EPlayMode.EditorSimulateMode)
            {
                Log.Info("当前为编辑器模式，直接进入 FsmGetGlobalInfoState");
                ChangeState<ProcedureGetAppVersionInfoFromGmServer>(procedureOwner);
                return;
            }

            // 离线模式--直接进入初始化YooAsset流程
            if (AssetManager.Instance.PlayMode == EPlayMode.OfflinePlayMode)
            {
                Log.Info("当前为离线模式，直接进入 ProcedurePatchInit");
                ChangeState<ProcedureUpdateInit>(procedureOwner);
                return;
            }

            // 热更模式
            GetGlobalInfo(procedureOwner);
        }

        /// <summary>
        /// 获取全局信息，包括：服务器地址、资源版本地址、内容信息s
        /// </summary>
        /// <param name="procedureOwner"></param>
        private async void GetGlobalInfo(IFsm<IProcedureManager> procedureOwner)
        {
            // 获取后台服务端全局信息的请求参数
            var reqBaseParams = HttpHelper.GetBaseParams();

            try
            {
                // 请求后台服务端，获取全局信息。
                var json = await GameApp.Web.PostToString(GlobalInfoUrl, reqBaseParams);
                Log.Info(json);

                var httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(json.Result);
                if (httpJsonResult.Code > 0)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("Server error, retrying...");
                    Log.Error($"获取全局信息返回异常=> Req:{reqBaseParams} Resp:{json}");

                    // 等待3秒后重新获取
                    await UniTask.Delay(3000);
                    GetGlobalInfo(procedureOwner);
                }
                else
                {
                    // 获取成功
                    var repGlobalInfo = Utility.Json.ToObject<ResponseGlobalInfo>(httpJsonResult.Data);

                    // 保存全局信息到globalConfigComponent组件中，供后续流程使用，特别是获取App版本号流程 与 获取资源版本号流程
                    var globalConfigComponent = GameApp.GlobalConfig;
                    globalConfigComponent.CheckAppVersionUrl      = repGlobalInfo.CheckAppVersionUrl;
                    globalConfigComponent.CheckResourceVersionUrl = repGlobalInfo.CheckResourceVersionUrl;
                    globalConfigComponent.Content                 = repGlobalInfo.Content;
                    LauncherUIHelper.SetTipText("Loading...");

                    // 进入获取App版本号流程
                    ChangeState<ProcedureGetAppVersionInfoFromGmServer>(procedureOwner);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                LauncherUIHelper.SetTipText("Network error, retrying...");
                Log.Error($"获取全局信息异常=>Error:{e.Message}   Req:{reqBaseParams}");

                // 等待3秒后重新获取
                await UniTask.Delay(3000);
                GetGlobalInfo(procedureOwner);
            }
        }
    }
}