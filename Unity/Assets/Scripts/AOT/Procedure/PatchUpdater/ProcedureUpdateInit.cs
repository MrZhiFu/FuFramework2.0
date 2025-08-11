using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using FuFramework.Core.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--资源更新初始化流程。
    /// 主要作用是：
    /// 1. 初始化设置YooAsset的资源包相关信息，包括：包名称、下载地址
    /// 2. 进入获取资源版本号流程
    /// </summary>
    public class ProcedureUpdateInit : ProcedureBase
    {
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 编辑器模拟模式下，直接进入 获取资源版本号流程
            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                Log.Info("当前为编辑器模拟模式，直接进入 获取资源包版本号流程");
                await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, string.Empty, string.Empty, true);
                ChangeState<ProcedureUpdateGetAssetPkgVersion>(procedureOwner);
                return;
            }

            // 离线模式下，直接进入 获取资源版本号流程
            if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
            {
                Log.Info("当前为离线模式，直接进入 获取资源包版本号流程");
                await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, string.Empty, string.Empty, true);
                ChangeState<ProcedureUpdateGetAssetPkgVersion>(procedureOwner);
                return;
            }

            // 网络模式下
            // 1.获取资源包的下载地址，并将下载地址初始化到YooAsset资源包相关信息中
            var downloadUrl = procedureOwner.GetData<VarString>(AssetComponent.BuildInPackageName);
            Log.Info("下载资源的路径：" + downloadUrl);
            await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, downloadUrl.Value, downloadUrl.Value, true);

            // 2.移除流程中的资源包路径数据
            procedureOwner.RemoveData(AssetComponent.BuildInPackageName);
            await UniTask.DelayFrame();

            // 3.进入获取资源包版本号流程
            ChangeState<ProcedureUpdateGetAssetPkgVersion>(procedureOwner);
        }
    }
}