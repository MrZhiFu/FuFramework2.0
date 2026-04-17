using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.Variable.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--获取资源包版本号流程。
    /// 功能：
    ///     1. 获取资源的版本号
    ///     2. 离线单机模式下，将最新版本号保存到流程的Data中，供再次使用，然后进入更新资源清单流程
    ///     3. 热更模式下，进入更新资源清单流程
    /// </summary>
    public class ProcedureGetPackageVersion : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 4;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：获取资源包版本------</color>");

            var assUpdateStateEventArgs = AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.GetVersion);
            GlobalModule.EventModule.Broadcast(this, assUpdateStateEventArgs);

            GetVersion().Forget();
        }

        /// <summary>
        /// 获取资源的版本号
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid GetVersion()
        {
            var package = GlobalModule.AssetModule.GetPackage(GlobalModule.AssetModule.DefaultPackageName);

            // 离线单机模式下请求的是应用程序内保存的版本号，版本号会随着YooAsset的打包一起生成，一般存放在StreamingAssets/yoo目录下，
            // 热更模式下请求的是资源服务器上的版本号，版本号会随着YooAsset的打包一起生成，一般存放在AssetBundle服务器上
            var operation = package.RequestPackageVersionAsync();
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 获取成功，保存版本号到流程中的Data变量"PackageVersion“中，用于后续更新资源清单流程
                var versionStr = ReferencePool.Acquire<VarString>();
                versionStr.SetValue(operation.PackageVersion);
                Fsm.SetData("PackageVersion", versionStr);

                // 进入更新资源清单流程
                FuLogger.LogInfo($"获取资源版本号成功 : {operation.PackageVersion}");
                ChangeState<ProcedureUpdatePackageManifest>();
            }
            else
            {
                // 获取失败，延迟3秒后重试
                FuLogger.LogError(operation.Error);
                GlobalModule.EventModule.Broadcast(this, AssetVersionUpdateFailedEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, operation.Error));

                await UniTask.WaitForSeconds(3);
                GetVersion().Forget();
            }
        }
    }
}