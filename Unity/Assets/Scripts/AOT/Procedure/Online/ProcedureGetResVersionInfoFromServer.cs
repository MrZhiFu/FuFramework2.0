using System;
using System.IO;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Web.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.GlobalConfig.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 获取服务端默认资源包的版本信息流程。
    /// 主要作用是：
    /// 1. 从后台服务端获取默认资源包的版本信息，包括：资源包名称、资源下载根路径、资源包版本号、平台、渠道等
    /// 2. 若获取成功，则将版本信息保存到流程管理器的Data变量中，并进入资源更新初始化流程。
    /// 3. 若获取失败，则等待一段时间后重新获取。
    /// </summary>
    public class ProcedureGetResVersionInfoFromServer : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入获取服务端默认资源包的版本信息流程-----</color>");
            
            ReqPackageVersionInfo(procedureOwner);
        }

        /// <summary>
        /// 请求服务端获取默认资源包的版本信息。
        /// </summary>
        /// <param name="procedureOwner"></param>
        private async void ReqPackageVersionInfo(IFsm<IProcedureManager> procedureOwner)
        {
            var jsonParams = HttpHelper.GetBaseParams();
            try
            {
                // 请求服务端，获取默认资源包的版本信息。
                jsonParams["AssetPackageName"] = AssetManager.Instance.DefaultPackageName;
                var result = await GameApp.Web.PostToString(GameApp.GlobalConfig.CheckResourceVersionUrl, jsonParams);
                
                Log.Info(result);

                var httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(result.Result);
                
                if (httpJsonResult.Code > 0)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("获取资源版本信息异常, 正在重试...");
                    Log.Error($"获取资源版本信息异常=> Req:{Utility.Json.ToJson(jsonParams)} Resp:{result}");
                    
                    // 若获取失败，延迟3秒后重试。
                    await UniTask.Delay(3000);
                    ReqPackageVersionInfo(procedureOwner);
                }
                else
                {
                    // 获取成功
                    var packageVersion = Utility.Json.ToObject<ResponseGameAssetPackageVersion>(httpJsonResult.Data);
                    
                    // 将资源下载路径保存到流程管理器的Data变量(DefaultPackage)中。
                    // downloadUrl = 资源下载根路径/资源包名称/平台/App版本号/渠道/资源包名称/资源包版本号
                    var downloadUrl = Path.Combine(packageVersion.RootPath, packageVersion.PackageName, packageVersion.Platform, packageVersion.AppVersion, packageVersion.Channel, packageVersion.AssetPackageName, packageVersion.Version) + Path.DirectorySeparatorChar;
                    var downloadUrlStr = ReferencePool.Acquire<VarString>();
                    downloadUrlStr.SetValue(downloadUrl);
                    procedureOwner.SetData(AssetManager.Instance.DefaultPackageName, downloadUrlStr);

                    // 将版本号保存到流程管理器的Data变量(DefaultPackageVersion)中，并进入热更流程--初始化资源包流程。
                    var versionStr = ReferencePool.Acquire<VarString>();
                    versionStr.SetValue(packageVersion.Version);
                    procedureOwner.SetData(AssetManager.Instance.DefaultPackageName + "Version", versionStr);
                    ChangeState<ProcedureUpdateInitPackage>(procedureOwner);
                }
            }
            catch (Exception e)
            {
                Log.Error($"获取资源版本信息异常=>Error:{e.Message}   Req:{Utility.Json.ToJson(jsonParams)}");
                LauncherUIHelper.SetTipText("获取资源版本信息异常, 正在重试...");
                await UniTask.Delay(3000);
                ReqPackageVersionInfo(procedureOwner);
            }
        }
    }
}