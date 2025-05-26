using System.Collections;
using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 更新资源版本号。
    /// 主要作用是：
    /// 1. 获取资源的版本号
    /// 2. 离线单机模式下，将最新版本号保存到流程的Data中，供再次使用，然后进入更新资源清单流程
    /// 3. 联机模式下，进入更新资源清单流程
    /// </summary>
    public class ProcedureUpdateVersion : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetComponent.BuildInPackageName, EPatchStates.UpdateStaticVersion));
            GetStaticVersion(procedureOwner).ToUniTask();
        }

        /// <summary>
        /// 获取资源的版本号
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator GetStaticVersion(IFsm<IProcedureManager> procedureOwner)
        {
            var buildInResourcePackage = YooAssets.GetPackage(AssetComponent.BuildInPackageName);

            // Package.RequestPackageVersionAsync()方法
            // 离线单机模式下请求的是应用程序内保存的版本号，一般存放在StreamingAssets目录下，
            // 联机模式下请求的是服务器上的版本号，一般存放在AssetBundle服务器上
            var buildInOperation = buildInResourcePackage.RequestPackageVersionAsync();
            yield return buildInOperation;

            if (buildInOperation.Status == EOperationStatus.Succeed)
            {
                // 获取成功
                var packageVersion = buildInOperation.PackageVersion;
                if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
                {
                    // 离线单机模式下，直接保存版本号到Data中
                    var varStringVersion = ReferencePool.Acquire<VarString>();
                    varStringVersion.SetValue(packageVersion);
                    procedureOwner.SetData(AssetComponent.BuildInPackageName + "Version", varStringVersion);
                }

                // 联机模式下，进入更新资源清单流程
                Log.Info($"更新资源版本号成功 : {packageVersion}");
                ChangeState<ProcedureUpdateManifest>(procedureOwner);
            }
            else
            {
                // 获取失败，再次进入自身流程尝试
                Log.Error(buildInOperation.Error);
                GameApp.Event.Fire(this, AssetStaticVersionUpdateFailedEventArgs.Create(AssetComponent.BuildInPackageName, buildInOperation.Error));
                ChangeState<ProcedureUpdateVersion>(procedureOwner);
            }
        }
    }
}