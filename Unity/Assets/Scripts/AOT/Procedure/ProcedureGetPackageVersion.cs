using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
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
    ///     1. 获取资源的版本号，将最新版本号保存到流程的Data中，供后续更新资源清单流程使用。
    ///     2. 进入更新资源清单流程。
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

            // 发送更新状态改变事件: 获取热更资源包版本号
            var updateStateChangeEvent = AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.GetVersion);
            GlobalModule.EventModule.Broadcast(this, updateStateChangeEvent);

            // 异步获取资源的版本号
            GetVersionAsync().Forget();
        }

        /// <summary>
        /// 异步获取资源的版本号
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid GetVersionAsync()
        {
            var package = GlobalModule.AssetModule.GetDefaultPackage();

            // 离线单机模式下请求的是应用程序内保存的版本号，版本号会随着YooAsset的打包一起生成，一般存放在StreamingAssets/yoo目录下，YooAsset会自动读取。
            // 热更模式下请求的是资源服务器上的版本号，版本号会随着YooAsset的打包一起生成，一般存放在AssetBundle服务器上，YooAsset会自动读取。
            var operation = package.RequestPackageVersionAsync();
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 获取成功，保存版本号到流程中的数据中，用于后续更新资源清单流程
                var versionStr = ReferencePool.Acquire<VarString>();
                versionStr.SetValue(operation.PackageVersion);
                Fsm.SetData("PackageVersion", versionStr);

                // 进入更新资源清单流程
                FuLogger.LogInfo($"获取资源版本号成功 : {operation.PackageVersion}");
                ChangeState<ProcedureUpdatePackageManifest>();
            }
            else
            {
                // 获取失败
                FuLogger.LogError($"获取资源版本号失败，3秒后重试: {operation.Error}");

                // 发送资源版本号更新失败事件
                var versionUpdateFailedEvent = AssetVersionUpdateFailedEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, operation.Error);
                GlobalModule.EventModule.Broadcast(this, versionUpdateFailedEvent);

                // 延迟3秒后重试
                await UniTask.WaitForSeconds(3);
                GetVersionAsync().Forget();
            }
        }
    }
}