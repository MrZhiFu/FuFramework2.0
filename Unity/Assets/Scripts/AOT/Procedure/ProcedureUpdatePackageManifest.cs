using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Variable.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--更新资源包清单流程。
    /// 功能：
    ///     1. 下载最新资源清单
    ///     2. 解析最新资源清单，并下载资源
    ///     3. 完成后创建资源下载器流程
    /// </summary>
    public class ProcedureUpdatePackageManifest : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 5;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：更新资源清单------</color>");

            // 发送更新状态改变事件: 更新资源清单
            var updateStateChangeEvent = AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.UpdateManifest);
            GlobalModule.EventModule.Broadcast(this, updateStateChangeEvent);

            // 更新资源清单
            UpdateManifest().Forget();
        }

        /// <summary>
        /// 更新资源清单
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid UpdateManifest()
        {
            var defaultPackage = GlobalModule.AssetModule.GetPackage(GlobalModule.AssetModule.DefaultPackageName);
            var versionStr     = Fsm.GetData<VarString>("PackageVersion");
            var operation      = defaultPackage.UpdatePackageManifestAsync(versionStr.Value);
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 更新成功
                //编辑器模拟模式或离线单机模式，则直接进入更新完成流程
                if (GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
                {
                    ChangeState<ProcedureUpdateDone>();
                    return;
                }

                // 热更模式，进入创建资源下载器流程
                ChangeState<ProcedureCreateDownloader>();
                Fsm.RemoveData("PackageVersion");
            }
            else
            {
                // 更新失败，发送资源更新下载失败事件，并重新尝试更新资源清单流程
                Debug.LogError(operation.Error);
                var manifestUpdateFailedEvent = AssetManifestUpdateFailedEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, operation.Error);
                GlobalModule.EventModule.Broadcast(this, manifestUpdateFailedEvent);
                ChangeState<ProcedureUpdatePackageManifest>();
            }
        }
    }
}