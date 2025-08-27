using YooAsset;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--更新资源清单流程。
    /// 主要作用是：
    /// 1. 更新资源清单
    /// 2. 完成后创建资源下载器流程
    /// </summary>
    public class ProcedureUpdatePackageManifest : ProcedureBase
    {
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------热更流程--更新资源清单流程-----</color>");
            
            if (AssetManager.Instance.PlayMode == EPlayMode.OfflinePlayMode)
            {
                // 离线单机模式下的更新资源清单
                var pkgVersion = procedureOwner.GetData<VarString>(AssetManager.Instance.DefaultPackageName + "Version");
                var package    = AssetManager.Instance.GetAssetsPackage(AssetManager.Instance.DefaultPackageName);
                var operation  = package.UpdatePackageManifestAsync(pkgVersion.Value);
                await operation.ToUniTask();
                ChangeState<ProcedureUpdateDone>(procedureOwner);
                return;
            }

            // 联机模式下的更新资源清单流程
            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.UpdateManifest));
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

            var buildInPackage = YooAssets.GetPackage(AssetManager.Instance.DefaultPackageName);
            UpdatePackageManifestOperation operation;

            if (AssetManager.Instance.PlayMode == EPlayMode.EditorSimulateMode)
            {
                // 编辑器模拟模式下，强制使用Simulate版本
                operation = buildInPackage.UpdatePackageManifestAsync("Simulate");
            }
            else
            {
                // 联机模式下，获取流程中存储的版本数据
                var versionStr = procedureOwner.GetData<VarString>(AssetManager.Instance.DefaultPackageName + "Version");
                operation = buildInPackage.UpdatePackageManifestAsync(versionStr.Value);
            }

            yield return operation;


            if (operation.Status == EOperationStatus.Succeed)
            {
                // 更新成功，进入创建资源下载器流程
                ChangeState<ProcedureUpdateCreateDownloader>(procedureOwner);
                procedureOwner.RemoveData(AssetManager.Instance.DefaultPackageName + "Version");
            }
            else
            {
                // 更新失败，重新尝试更新资源清单流程
                Debug.LogError(operation.Error);
                GameApp.Event.Fire(this, AssetPatchManifestUpdateFailedEventArgs.Create(AssetManager.Instance.DefaultPackageName, operation.Error));
                ChangeState<ProcedureUpdatePackageManifest>(procedureOwner);
            }
        }
    }
}