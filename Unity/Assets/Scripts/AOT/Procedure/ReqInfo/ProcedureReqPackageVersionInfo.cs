using System;
using System.IO;
using Cysharp.Threading.Tasks;
using FuFramework.Web.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.GlobalConfig.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 获取服务端默认资源包的版本信息流程。
    /// 主要作用是：
    /// 1. 从服务端获取默认资源包的版本信息，包括：资源包名称、资源下载根路径、资源包版本号、平台、渠道等
    /// 2. 若获取成功，则将版本信息保存到流程管理器的Data变量中，并进入资源更新初始化流程。
    /// 3. 若获取失败，则等待一段时间后重新获取。
    /// </summary>
    public class ProcedureReqPackageVersionInfo : ProcedureBase
    {
        public override int Priority => 4; // 优先级。

        protected override void OnEnter()
        {
            base.OnEnter();
            Log.Info("<color=#43f656>------进入获取服务端默认资源包的版本信息流程------</color>");

            GetAssetPackageVersionInfo().Forget();
        }

        /// <summary>
        /// 从服务端获取默认资源包的版本信息。
        /// </summary>
        private async UniTaskVoid GetAssetPackageVersionInfo()
        {
            var jsonParams = HttpHelper.GetBaseParams();
            try
            {
                // 请求服务端，获取默认资源包的版本信息。
                jsonParams["AssetPackageName"] = GlobalModule.AssetModule.DefaultPackageName;
                var rstJson = await GlobalModule.WebModule.PostToString(GlobalModule.GlobalConfigModule.CheckResourceVersionUrl, jsonParams);
                Log.Info(rstJson);

                var httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(rstJson.Result);
                if (httpJsonResult.Code > 0)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("获取资源版本信息异常, 正在重试...");
                    Log.Error($"获取资源版本信息异常=> Req:{Utility.Json.ToJson(jsonParams)} Resp:{rstJson}");

                    // 若获取失败，延迟3秒后重试。
                    await UniTask.WaitForSeconds(3);
                    GetAssetPackageVersionInfo().Forget();
                }
                else
                {
                    // 获取成功
                    var assetPackageVersion = Utility.Json.ToObject<ResponseGameAssetPackageVersion>(httpJsonResult.Data);

                    // 将资源下载路径保存到流程管理器的Data变量("DownloadURL")中，路径格式为：根路径/资源包名称/平台/版本号/渠道/资源包名称/版本号
                    var downloadURL = Path.Combine(assetPackageVersion.RootPath, assetPackageVersion.PackageName, assetPackageVersion.Platform, assetPackageVersion.AppVersion,
                                                   assetPackageVersion.Channel, assetPackageVersion.AssetPackageName, assetPackageVersion.Version) + Path.DirectorySeparatorChar;
                    var downloadURLStr = ReferencePool.Acquire<VarString>();
                    downloadURLStr.SetValue(downloadURL);
                    Fsm.SetData("DownloadURL", downloadURLStr);

                    // 将版本信息保存到流程管理器的Data变量("PackageVersion)中，进入资源更新初始化流程。
                    var versionStr = ReferencePool.Acquire<VarString>();
                    versionStr.SetValue(assetPackageVersion.Version);
                    Fsm.SetData("PackageVersion", versionStr);
                    ChangeState<ProcedureInitPackage>();
                }
            }
            catch (Exception e)
            {
                Log.Error($"获取资源版本信息异常=>Error:{e.Message}   Req:{Utility.Json.ToJson(jsonParams)}");
                LauncherUIHelper.SetTipText("获取资源版本信息异常, 正在重试...");
                await UniTask.WaitForSeconds(3);
                GetAssetPackageVersionInfo().Forget();
            }
        }
    }
}