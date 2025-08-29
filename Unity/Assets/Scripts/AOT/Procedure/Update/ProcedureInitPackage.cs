using YooAsset;
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
    /// 热更流程--初始化资源包流程。
    /// 主要作用是：
    /// 1. 初始化设置资源包相关信息，包括：包名称、下载地址
    /// 2. 进入获取资源版本号流程
    /// </summary>
    public class ProcedureInitPackage : ProcedureBase
    {
        public override int Priority => 5; // 显示优先级

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：初始化资源包------</color>");
            
            InitPackage(procedureOwner).Forget();
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        /// <param name="procedureOwner"></param>
        private async UniTaskVoid InitPackage(IFsm<IProcedureManager> procedureOwner)
        {
            // 编辑器模拟模式/单机离线模式下，初始化完毕后直接进入获取资源版本号流程
            if (GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
            {
                await GlobalModule.AssetModule.InitPackageAsync(GlobalModule.AssetModule.DefaultPackageName);
                ChangeState<ProcedureGetPackageVersion>(procedureOwner);
                return;
            }

            // 热更模式下
            // 获取资源包的下载地址，并将下载地址传入初始化资源包方法中，同时移除流程中的下载地址数据，初始化完毕后直接进入获取资源版本号流程
            var downloadURL = procedureOwner.GetData<VarString>("DownloadURL");
            Log.Info($"资源包的下载路径：{downloadURL}");

            await GlobalModule.AssetModule.InitPackageAsync(GlobalModule.AssetModule.DefaultPackageName, downloadURL.Value, downloadURL.Value);

            procedureOwner.RemoveData("DownloadURL");
            await UniTask.DelayFrame();

            ChangeState<ProcedureGetPackageVersion>(procedureOwner);
        }
    }
}