using System;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Web.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Entry.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 获取服务端全局信息流程。
    /// 主要作用是：
    /// 1. 获取全局信息，包括：App版本号检查地址、资源版本检查地址、额外内容信息
    /// 2. 获取成功，保存全局信息到globalConfigComponent组件中，并进入获取App版本号流程
    /// 3. 若获取失败，则提示网络异常，并延迟3秒后重试。。 
    ///  </summary>
    public class ProcedureReqGlobalInfo : ProcedureBase
    {
        public override int Priority => 2; // 显示优先级
        
        /// <summary>
        /// 全局信息的服务器地址
        /// </summary>
        private const string GlobalInfoUrl = "http://127.0.0.1:20808/api/GameGlobalInfo/GetInfo";

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入获取服务端全局信息流程------</color>");
            
            // 热更模式
            GetGlobalInfo(procedureOwner).Forget();
        }

        /// <summary>
        /// 获取全局信息，包括：服务器地址、资源版本地址、内容信息s
        /// </summary>
        /// <param name="procedureOwner"></param>
        private async UniTaskVoid GetGlobalInfo(IFsm<IProcedureManager> procedureOwner)
        {
            // 获取服务端全局信息的请求参数
            var reqBaseParams = HttpHelper.GetBaseParams();

            try
            {
                // 请求服务端，获取全局信息。
                var json = await GlobalModule.WebModule.PostToString(GlobalInfoUrl, reqBaseParams);
                Log.Info(json);

                var httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(json.Result);
                if (httpJsonResult.Code > 0)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("Server error, retrying...");
                    Log.Error($"获取全局信息返回异常=> Req:{reqBaseParams} Resp:{json}");

                    // 等待3秒后重新获取
                    await UniTask.WaitForSeconds(3);
                    GetGlobalInfo(procedureOwner).Forget();
                }
                else
                {
                    // 获取成功，保存全局信息到globalConfigComponent组件中，供后续流程使用，特别是获取App版本号流程 与 获取资源版本号流程
                    var repGlobalInfo = Utility.Json.ToObject<ResponseGlobalInfo>(httpJsonResult.Data);
                    var globalConfigComponent = GlobalModule.GlobalConfigModule;
                    globalConfigComponent.CheckAppVersionUrl      = repGlobalInfo.CheckAppVersionUrl;
                    globalConfigComponent.CheckResourceVersionUrl = repGlobalInfo.CheckResourceVersionUrl;
                    globalConfigComponent.Content                 = repGlobalInfo.Content;
                    LauncherUIHelper.SetTipText("Loading...");

                    // 进入获取App版本号流程
                    ChangeState<ProcedureReqAppVersionInfo>(procedureOwner);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                LauncherUIHelper.SetTipText("Network error, retrying...");
                Log.Error($"获取全局信息异常=>Error:{e.Message}   Req:{reqBaseParams}");

                // 等待3秒后重新获取
                await UniTask.WaitForSeconds(3);
                GetGlobalInfo(procedureOwner).Forget();
            }
        }
    }
}