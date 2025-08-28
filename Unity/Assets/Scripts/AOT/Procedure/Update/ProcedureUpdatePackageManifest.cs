using YooAsset;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--更新资源包清单流程。
    /// 主要作用是：
    /// 1. 下载最新资源清单
    /// 2. 解析最新资源清单，并下载资源
    /// 3. 完成后创建资源下载器流程
    /// </summary>
    public class ProcedureUpdatePackageManifest : ProcedureBase
    {
        public override int Priority => 7; // 显示优先级

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：更新资源清单------</color>");

            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.UpdateManifest));
            UpdateManifest(procedureOwner).ToUniTask().Forget();
        }
        
        /// <summary>
        /// 更新资源清单
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator UpdateManifest(IFsm<IProcedureManager> procedureOwner)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            var defaultPackage = AssetManager.Instance.GetPackage(AssetManager.Instance.DefaultPackageName);
            var versionStr = procedureOwner.GetData<VarString>("PackageVersion");
            var operation = defaultPackage.UpdatePackageManifestAsync(versionStr.Value);
            yield return operation;
            
            if (operation.Status == EOperationStatus.Succeed)
            {
                // 更新成功，如果是编辑器模拟模式或离线单机模式，则直接进入更新完成流程
                if (AssetManager.Instance.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
                {
                    ChangeState<ProcedureUpdateDone>(procedureOwner);
                    yield break;
                }

                // 热更模式，进入创建资源下载器流程
                ChangeState<ProcedureCreateDownloader>(procedureOwner);
                procedureOwner.RemoveData("PackageVersion");
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