using YooAsset;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Variable.Runtime;

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

        protected override void OnEnter()
        {
            base.OnEnter();
            Log.Info("<color=#43f656>------进入热更流程：更新资源清单------</color>");

            GlobalModule.EventModule.Fire(this, AssetPatchStatesChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EPatchStates.UpdateManifest));
            UpdateManifest().ToUniTask().Forget();
        }

        /// <summary>
        /// 更新资源清单
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateManifest()
        {
            yield return new WaitForSecondsRealtime(0.1f);

            var defaultPackage = GlobalModule.AssetModule.GetPackage(GlobalModule.AssetModule.DefaultPackageName);
            var versionStr     = Fsm.GetData<VarString>("PackageVersion");
            var operation      = defaultPackage.UpdatePackageManifestAsync(versionStr.Value);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                // 更新成功，如果是编辑器模拟模式或离线单机模式，则直接进入更新完成流程
                if (GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
                {
                    ChangeState<ProcedureUpdateDone>();
                    yield break;
                }

                // 热更模式，进入创建资源下载器流程
                ChangeState<ProcedureCreateDownloader>();
                Fsm.RemoveData("PackageVersion");
            }
            else
            {
                // 更新失败，重新尝试更新资源清单流程
                Debug.LogError(operation.Error);
                GlobalModule.EventModule.Fire(this, AssetPatchManifestUpdateFailedEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, operation.Error));
                ChangeState<ProcedureUpdatePackageManifest>();
            }
        }
    }
}