using System.Collections;
using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using YooAsset;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--获取资源包版本号流程。
    /// 主要作用是：
    /// 1. 获取资源的版本号
    /// 2. 离线单机模式下，将最新版本号保存到流程的Data中，供再次使用，然后进入更新资源清单流程
    /// 3. 联机模式下，进入更新资源清单流程
    /// </summary>
    public class ProcedureUpdateGetAssetPkgVersion : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetComponent.BuildInPackageName, EPatchStates.UpdateStaticVersion));
            GetVersion(procedureOwner).ToUniTask();
        }

        /// <summary>
        /// 获取资源的版本号
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator GetVersion(IFsm<IProcedureManager> procedureOwner)
        {
            var package = GameApp.Asset.GetAssetsPackage(AssetComponent.BuildInPackageName);

            // Package.RequestPackageVersionAsync()方法
            // 离线单机模式下请求的是应用程序内保存的版本号，一般存放在StreamingAssets目录下，
            // 联机模式下请求的是服务器上的版本号，一般存放在AssetBundle服务器上
            var operation = package.RequestPackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 获取成功
                var packageVersion = operation.PackageVersion;
                if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
                {
                    // 离线单机模式下，保存版本号到流程中的Data中
                    var versionStr = ReferencePool.Acquire<VarString>();
                    versionStr.SetValue(packageVersion);
                    procedureOwner.SetData(AssetComponent.BuildInPackageName + "Version", versionStr);
                }

                // 进入更新资源清单流程
                Log.Info($"获取资源版本号成功 : {packageVersion}");
                ChangeState<ProcedureUpdateManifest>(procedureOwner);
            }
            else
            {
                // 获取失败，再次进入自身流程尝试
                Log.Error(operation.Error);
                GameApp.Event.Fire(this, AssetStaticVersionUpdateFailedEventArgs.Create(AssetComponent.BuildInPackageName, operation.Error));
                ChangeState<ProcedureUpdateGetAssetPkgVersion>(procedureOwner);
            }
        }
    }
}