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

            // 异步初始化资源包
            InitPackageAsync().Forget();
        }

        /// <summary>
        /// 异步初始化资源包
        /// </summary>
        private async UniTaskVoid InitPackageAsync()
        {
            // 编辑器模拟模式/单机离线模式下，初始化完毕后直接进入获取资源版本号流程
            if (GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
            {
                // 初始化默认资源包
                await GlobalModule.AssetModule.InitDefaultPackageAsync();
                ChangeState<ProcedureGetPackageVersion>();
                return;
            }

            // 热更模式下，获取更新配置信息，并取得其中资源包的下载地址和备用下载地址，然后初始化默认资源包
            if (Fsm.GetData<VarObject>("UpdateConfig").Value is not RemoteUpdateConfig updateConfig)
            {
                FuLogger.LogError("获取资源包的配置信息失败，请检查网络连接或配置信息是否正确！");
                return;
            }

            // App主次版本号：如 v1.0
            var version = $"v{Utility.Version.MajorMinorVersion}";

            // 资源包的下载地址和备用下载地址，格式如：http://127.0.0.1:8080/CDN/Android/{v1.0}/
            var downloadUrl       = string.Format(updateConfig.ResDownloadUrl,       version);
            var downloadBackupUrl = string.Format(updateConfig.ResDownloadBackupUrl, version);
            FuLogger.LogInfo($"资源包的下载地址：{downloadUrl}，备用下载地址：{downloadBackupUrl}");

            // 初始化默认资源包
            await GlobalModule.AssetModule.InitDefaultPackageAsync(downloadUrl, downloadBackupUrl);

            // 移除流程中的保存的下载地址数据
            // Fsm.RemoveData("ResDownloadUrl");
            // Fsm.RemoveData("ResDownloadBackupUrl");

            // 等待一帧
            await UniTask.NextFrame();

            // 进入获取资源版本号流程
            ChangeState<ProcedureGetPackageVersion>();
        }
    }
}