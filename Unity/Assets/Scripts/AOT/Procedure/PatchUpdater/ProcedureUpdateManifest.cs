using System.Collections;
using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using FuFramework.Core.Runtime;
using UnityEngine;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--更新资源清单流程。
    /// 主要作用是：
    /// 1. 下载最新资源清单
    /// 2. 解析最新资源清单，并下载资源
    /// 3. 完成后创建资源下载器流程
    /// </summary>
    public class ProcedureUpdateManifest : ProcedureBase
    {
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
            {
                // 离线单机模式下的更新资源清单
                var versionStr = procedureOwner.GetData<VarString>(AssetComponent.BuildInPackageName + "Version");
                var package    = GameApp.Asset.GetAssetsPackage(AssetComponent.BuildInPackageName);
                var operation  = package.UpdatePackageManifestAsync(versionStr.Value);
                await operation.ToUniTask();
                ChangeState<ProcedureUpdateDone>(procedureOwner);
                return;
            }

            // 联机模式下的更新资源清单流程
            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetComponent.BuildInPackageName, EPatchStates.UpdateManifest));
            await UpdateManifest(procedureOwner).ToUniTask();
        }


        /// <summary>
        /// 更新资源清单
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator UpdateManifest(IFsm<IProcedureManager> procedureOwner)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            var buildInPackage = YooAssets.GetPackage(AssetComponent.BuildInPackageName);
            UpdatePackageManifestOperation operation;

            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                // 编辑器模拟模式下，强制使用Simulate版本
                operation = buildInPackage.UpdatePackageManifestAsync("Simulate");
            }
            else
            {
                // 联机模式下，获取流程中存储的版本数据
                var versionStr = procedureOwner.GetData<VarString>(AssetComponent.BuildInPackageName + "Version");
                operation = buildInPackage.UpdatePackageManifestAsync(versionStr.Value);
            }

            yield return operation;


            if (operation.Status == EOperationStatus.Succeed)
            {
                // 更新成功，进入创建资源下载器流程
                ChangeState<ProcedureUpdateCreateDownloader>(procedureOwner);
                procedureOwner.RemoveData(AssetComponent.BuildInPackageName + "Version");
            }
            else
            {
                // 更新失败，重新尝试更新资源清单流程
                Debug.LogError(operation.Error);
                GameApp.Event.Fire(this, AssetPatchManifestUpdateFailedEventArgs.Create(AssetComponent.BuildInPackageName, operation.Error));
                ChangeState<ProcedureUpdateManifest>(procedureOwner);
            }
        }
    }
}