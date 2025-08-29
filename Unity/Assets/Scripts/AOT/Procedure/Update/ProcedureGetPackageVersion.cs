using YooAsset;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Entry.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--获取资源包版本号流程。
    /// 主要作用是：
    /// 1. 获取资源的版本号
    /// 2. 离线单机模式下，将最新版本号保存到流程的Data中，供再次使用，然后进入更新资源清单流程
    /// 3. 热更模式下，进入更新资源清单流程
    /// </summary>
    public class ProcedureGetPackageVersion : ProcedureBase
    {
        public override int Priority => 6; // 显示优先级

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：获取资源包版本------</color>");

            GlobalModule.EventModule.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.UpdateVersion));
            GetVersion(procedureOwner).ToUniTask().Forget();
        }

        /// <summary>
        /// 获取资源的版本号
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator GetVersion(IFsm<IProcedureManager> procedureOwner)
        {
            var package = AssetManager.Instance.GetPackage(AssetManager.Instance.DefaultPackageName);

            // 离线单机模式下请求的是应用程序内保存的版本号，一般存放在StreamingAssets目录下，
            // 热更模式下请求的是服务器上的版本号，一般存放在AssetBundle服务器上
            var operation = package.RequestPackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 获取成功，保存版本号到流程中的Data变量"PackageVersion“中，用于后续更新资源清单流程
                var versionStr = ReferencePool.Acquire<VarString>();
                versionStr.SetValue(operation.PackageVersion);
                procedureOwner.SetData("PackageVersion", versionStr);

                // 进入更新资源清单流程
                Log.Info($"获取资源版本号成功 : {operation.PackageVersion}");
                ChangeState<ProcedureUpdatePackageManifest>(procedureOwner);
            }
            else
            {
                // 获取失败，再次进入自身流程尝试
                Log.Error(operation.Error);
                GlobalModule.EventModule.Fire(this, AssetStaticVersionUpdateFailedEventArgs.Create(AssetManager.Instance.DefaultPackageName, operation.Error));
                ChangeState<ProcedureGetPackageVersion>(procedureOwner);
            }
        }
    }
}