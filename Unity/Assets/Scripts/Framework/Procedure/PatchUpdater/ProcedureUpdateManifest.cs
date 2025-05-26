using System.Collections;
using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using UnityEngine;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 更新资源清单流程。
    /// 主要作用是：
    /// 1. 下载最新资源清单
    /// 2. 解析最新资源清单，并下载资源
    /// 3. 完成后创建资源下载器流程
    /// </summary>
    internal sealed class ProcedureUpdateManifest : ProcedureBase
    {
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
            {
                // 离线单机模式下
                var versionStr       = procedureOwner.GetData<VarString>(AssetComponent.BuildInPackageName + "Version");
                var buildInPackage   = GameApp.Asset.GetAssetsPackage(AssetComponent.BuildInPackageName);
                var buildInOperation = buildInPackage.UpdatePackageManifestAsync(versionStr.Value);
                await buildInOperation.ToUniTask();
                ChangeState<ProcedurePatchDone>(procedureOwner);
                return;
            }

            // 联机模式下
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

            UpdatePackageManifestOperation buildInOperation;

            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                // 编辑器模拟模式下，强制使用Simulate版本
                buildInOperation = buildInPackage.UpdatePackageManifestAsync("Simulate");
            }
            else
            {
                // 联机模式下，获取流程中存储的版本号
                var versionStr = procedureOwner.GetData<VarString>(AssetComponent.BuildInPackageName + "Version");
                buildInOperation = buildInPackage.UpdatePackageManifestAsync(versionStr.Value);
            }

            yield return buildInOperation;


            if (buildInOperation.Status == EOperationStatus.Succeed)
            {
                // 更新成功，进入创建资源下载器流程
                ChangeState<ProcedureUpdateCreateDownloader>(procedureOwner);
                procedureOwner.RemoveData(AssetComponent.BuildInPackageName + "Version");
            }
            else
            {
                // 更新失败，重新尝试更新资源清单流程
                Debug.LogError(buildInOperation.Error);
                GameApp.Event.Fire(this, AssetPatchManifestUpdateFailedEventArgs.Create(AssetComponent.BuildInPackageName, buildInOperation.Error));
                ChangeState<ProcedureUpdateManifest>(procedureOwner);
            }
        }
    }
}