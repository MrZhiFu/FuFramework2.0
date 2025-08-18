using System;
using System.IO;
using Cysharp.Threading.Tasks;
using FuFramework.Asset.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using GameFrameX.Web.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 从后台服务端获取默认资源包的版本信息流程。
    /// 主要作用是：
    /// 1. 从后台服务端获取默认资源包的版本信息，包括：资源包名称、资源下载根路径、资源包版本号、平台、渠道等
    /// 2. 若获取成功，则将版本信息保存到流程管理器的Data变量中，并进入资源更新初始化流程。
    /// 3. 若获取失败，则等待一段时间后重新获取。
    /// </summary>
    public class ProcedureGetAssetPkgVersionInfoFromGmServer : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GetAssetPackageVersionInfo(procedureOwner);
        }

        /// <summary>
        /// 从后台服务端获取默认资源包的版本信息。
        /// </summary>
        /// <param name="procedureOwner"></param>
        private async void GetAssetPackageVersionInfo(IFsm<IProcedureManager> procedureOwner)
        {
            var jsonParams = HttpHelper.GetBaseParams();
            try
            {
                // 请求后台服务端，获取默认资源包的版本信息。
                jsonParams["AssetPackageName"] = AssetComponent.BuildInPackageName;
                var rstJson = await GameApp.Web.PostToString(GameApp.GlobalConfig.CheckResourceVersionUrl, jsonParams);
                
                Log.Info(rstJson);

                var httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(rstJson.Result);
                
                if (httpJsonResult.Code > 0)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("获取资源版本信息异常, 正在重试...");
                    Log.Error($"获取资源版本信息异常=> Req:{Utility.Json.ToJson(jsonParams)} Resp:{rstJson}");
                    
                    // 若获取失败，延迟3秒后重试。
                    await UniTask.Delay(3000);
                    GetAssetPackageVersionInfo(procedureOwner);
                }
                else
                {
                    // 获取成功
                    var assetPackageVersion = Utility.Json.ToObject<ResponseGameAssetPackageVersion>(httpJsonResult.Data);
                    
                    // 将资源下载路径保存到流程管理器的Data变量(DefaultPackage)中。
                    var downloadUrl = Path.Combine(assetPackageVersion.RootPath, assetPackageVersion.PackageName, assetPackageVersion.Platform, assetPackageVersion.AppVersion, assetPackageVersion.Channel, assetPackageVersion.AssetPackageName, assetPackageVersion.Version) + Path.DirectorySeparatorChar;
                    var downloadUrlStr = ReferencePool.Acquire<VarString>();
                    downloadUrlStr.SetValue(downloadUrl);
                    procedureOwner.SetData(AssetComponent.BuildInPackageName, downloadUrlStr);

                    // 将版本信息保存到流程管理器的Data变量(DefaultPackageVersion)中，并进入资源更新初始化流程。
                    var versionStr = ReferencePool.Acquire<VarString>();
                    versionStr.SetValue(assetPackageVersion.Version);
                    procedureOwner.SetData(AssetComponent.BuildInPackageName + "Version", versionStr);
                    ChangeState<ProcedureUpdateInit>(procedureOwner);
                }
            }
            catch (Exception e)
            {
                Log.Error($"获取资源版本信息异常=>Error:{e.Message}   Req:{Utility.Json.ToJson(jsonParams)}");
                LauncherUIHelper.SetTipText("获取资源版本信息异常, 正在重试...");
                await UniTask.Delay(3000);
                GetAssetPackageVersionInfo(procedureOwner);
            }
        }
    }
}