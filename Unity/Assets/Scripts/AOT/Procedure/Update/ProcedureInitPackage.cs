using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Variable.Runtime;

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

        protected override void OnEnter()
        {
            base.OnEnter();
            Log.Info("<color=#43f656>------进入热更流程：初始化资源包------</color>");

            InitPackage().Forget();
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        private async UniTaskVoid InitPackage()
        {
            // 编辑器模拟模式/单机离线模式下，初始化完毕后直接进入获取资源版本号流程
            if (GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
            {
                await GlobalModule.AssetModule.InitPackageAsync(GlobalModule.AssetModule.DefaultPackageName);
                ChangeState<ProcedureGetPackageVersion>();
                return;
            }

            // 热更模式下
            // 获取资源包的下载地址，并将下载地址传入初始化资源包方法中，同时移除流程中的下载地址数据，初始化完毕后直接进入获取资源版本号流程
            var downloadURL = Fsm.GetData<VarString>("DownloadURL");
            Log.Info($"资源包的下载路径：{downloadURL}");

            await GlobalModule.AssetModule.InitPackageAsync(GlobalModule.AssetModule.DefaultPackageName, downloadURL.Value, downloadURL.Value);

            Fsm.RemoveData("DownloadURL");
            await UniTask.DelayFrame();

            ChangeState<ProcedureGetPackageVersion>();
        }
    }
}