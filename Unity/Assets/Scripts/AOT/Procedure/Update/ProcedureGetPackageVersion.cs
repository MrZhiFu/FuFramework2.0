using YooAsset;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
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
    public class ProcedureGetPackageVersion : ProcedureBase
    {
        public override int Priority => 6; // 显示优先级
        
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：获取资源包版本------</color>");
            
            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.UpdateStaticVersion));
            GetVersion(procedureOwner).ToUniTask();
        }

        /// <summary>
        /// 获取资源的版本号
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator GetVersion(IFsm<IProcedureManager> procedureOwner)
        {
            var package = AssetManager.Instance.GetAssetsPackage(AssetManager.Instance.DefaultPackageName);

            // Package.RequestPackageVersionAsync()方法
            // 离线单机模式下请求的是应用程序内保存的版本号，一般存放在StreamingAssets目录下，
            // 联机模式下请求的是服务器上的版本号，一般存放在AssetBundle服务器上
            var operation = package.RequestPackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 获取成功
                var packageVersion = operation.PackageVersion;
                if (AssetManager.Instance.PlayMode == EPlayMode.OfflinePlayMode)
                {
                    // 离线单机模式下，保存版本号到流程中的Data中
                    var versionStr = ReferencePool.Acquire<VarString>();
                    versionStr.SetValue(packageVersion);
                    procedureOwner.SetData(AssetManager.Instance.DefaultPackageName + "Version", versionStr);
                }

                // 进入更新资源清单流程
                Log.Info($"获取资源版本号成功 : {packageVersion}");
                ChangeState<ProcedureUpdatePackageManifest>(procedureOwner);
            }
            else
            {
                // 获取失败，再次进入自身流程尝试
                Log.Error(operation.Error);
                GameApp.Event.Fire(this, AssetStaticVersionUpdateFailedEventArgs.Create(AssetManager.Instance.DefaultPackageName, operation.Error));
                ChangeState<ProcedureGetPackageVersion>(procedureOwner);
            }
        }
    }
}