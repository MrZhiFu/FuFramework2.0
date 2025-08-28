using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.UI.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 启动流程
    /// 主要作用是：
    /// 1. 设置FairyGUI的Loader加载器为自定义加载器
    /// 2. 启动UI
    /// 3. 进入获取全局信息流程
    /// </summary>
    public class ProcedureLauncher : ProcedureBase
    {
        public override int Priority => 1; // 显示优先级

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入首次启动流程------</color>");

            // 设置FairyGUI的Loader加载器为自定义加载器
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));
            
            LauncherUIHelper.Start().Forget(); // 启动热更进度UI
            Start(procedureOwner).Forget();    // 启动流程
        }
        
        /// <summary>
        /// 进入获取全局信息流程
        /// </summary>
        private async UniTaskVoid Start(IFsm<IProcedureManager> procedureOwner)
        {
            await UniTask.NextFrame();

            // 编辑器下的模拟模式/单机离线模式--进入初始化资源包流程
            if (AssetManager.Instance.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
            {
                ChangeState<ProcedureInitPackage>(procedureOwner);
                return;
            }

            // 热更模式--进入获取服务端全局信息流程
            ChangeState<ProcedureReqGlobalInfo>(procedureOwner);
        }
    }
}