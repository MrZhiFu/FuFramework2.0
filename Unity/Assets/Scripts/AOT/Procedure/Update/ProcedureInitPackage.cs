using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.ModuleSetting.Runtime;

namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--初始化资源包流程。
    /// 主要作用是：
    /// 1. 初始化设置YooAsset的资源包相关信息，包括：包名称、下载地址
    /// 2. 进入获取资源版本号流程
    /// </summary>
    public class ProcedureInitPackage : ProcedureBase
    {
        public override int Priority => 5; // 显示优先级
        
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：初始化资源包------</color>");
            
            // 获取资源模块配置数据
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            if (!assetSetting) throw new FuException("资源模块配置数据为空!");

            // 编辑器模拟模式下，直接进入 获取资源版本号流程
            if (AssetManager.Instance.PlayMode == EPlayMode.EditorSimulateMode)
            {
                Log.Info("当前为编辑器模拟模式，直接进入 获取资源包版本号流程");


                // 遍历配置，初始化所有资源包
                foreach (var packageInfo in assetSetting.AllPackages)
                {
                    await AssetManager.Instance.InitPackageAsync(packageInfo.PackageName, packageInfo.DownloadURL, packageInfo.FallbackDownloadURL, packageInfo.IsDefaultPackage);
                }

                // await AssetManager.Instance.InitPackageAsync(AssetManager.Instance.DefaultPackageName, string.Empty, string.Empty, true);
                ChangeState<ProcedureGetPackageVersion>(procedureOwner);
                return;
            }

            // 离线模式下，直接进入 获取资源版本号流程
            if (AssetManager.Instance.PlayMode == EPlayMode.OfflinePlayMode)
            {
                Log.Info("当前为离线模式，直接进入 获取资源包版本号流程");

                // 遍历配置，初始化所有资源包
                foreach (var packageInfo in assetSetting.AllPackages)
                {
                    await AssetManager.Instance.InitPackageAsync(packageInfo.PackageName, packageInfo.DownloadURL, packageInfo.FallbackDownloadURL, packageInfo.IsDefaultPackage);
                }

                // await AssetManager.Instance.InitPackageAsync(AssetManager.Instance.DefaultPackageName, string.Empty, string.Empty, true);
                ChangeState<ProcedureGetPackageVersion>(procedureOwner);
                return;
            }

            // 网络模式下
            // 1.获取资源包的下载地址，并将下载地址初始化到YooAsset资源包相关信息中
            // var downloadUrl = procedureOwner.GetData<VarString>(AssetManager.Instance.DefaultPackageName);
            // Log.Info("下载资源的路径：" + downloadUrl);
            // await AssetManager.Instance.InitPackageAsync(AssetManager.Instance.DefaultPackageName, downloadUrl.Value, downloadUrl.Value, true);

            // // 2.移除流程中的资源包路径数据
            // procedureOwner.RemoveData(AssetManager.Instance.DefaultPackageName);

            // 遍历配置，初始化所有资源包
            foreach (var packageInfo in assetSetting.AllPackages)
            {
                await AssetManager.Instance.InitPackageAsync(packageInfo.PackageName, packageInfo.DownloadURL, packageInfo.FallbackDownloadURL, packageInfo.IsDefaultPackage);
            }

            await UniTask.DelayFrame();

            // 3.进入获取资源包版本号流程
            ChangeState<ProcedureGetPackageVersion>(procedureOwner);
        }
    }
}