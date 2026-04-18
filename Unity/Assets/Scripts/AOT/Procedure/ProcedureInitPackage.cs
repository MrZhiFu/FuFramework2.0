using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Variable.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--初始化资源包流程。
    /// 功能：
    ///     1. 初始化默认资源包相关信息，包括：资源包名称、资源包下载地址，资源包备用下载地址。
    ///     2. 进入获取资源版本号流程。
    /// 注意：
    /// </summary>
    public class ProcedureInitPackage : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 3;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：初始化资源包------</color>");

            // 初始化资源包
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
                // 初始化默认资源包
                await GlobalModule.AssetModule.InitDefaultPackageAsync();
                ChangeState<ProcedureGetPackageVersion>();
                return;
            }

            // 热更模式下
            // 获取资源包的下载地址和备用下载地址
            var downloadUrl       = Fsm.GetData<VarString>("ResDownloadUrl");
            var downloadBackupUrl = Fsm.GetData<VarString>("ResDownloadBackupUrl");
            FuLogger.LogInfo($"资源包的下载地址：{downloadUrl}, 备用下载地址：{downloadBackupUrl}");

            // 初始化默认资源包
            await GlobalModule.AssetModule.InitDefaultPackageAsync(downloadUrl.Value, downloadBackupUrl.Value);

            // 移除流程中的保存的下载地址数据
            Fsm.RemoveData("ResDownloadUrl");
            Fsm.RemoveData("ResDownloadBackupUrl");

            // 等待一帧
            await UniTask.NextFrame();

            // 进入获取资源版本号流程
            ChangeState<ProcedureGetPackageVersion>();
        }
    }
}