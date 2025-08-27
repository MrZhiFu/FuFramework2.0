using Cysharp.Threading.Tasks;
using FuFramework.Asset.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Procedure.Runtime;
using YooAsset;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Unity.Startup.Procedure
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
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入Launcher流程-----</color>");
            
            // 设置FairyGUI的Loader加载器为自定义加载器
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));
            
            // 启动UI
            LauncherUIHelper.Start().Forget();
            
            // 启动流程
            Start(procedureOwner).Forget();
        }

        
        /// <summary>
        /// 根据当前的资源运行模式，进入不同的流程
        /// </summary>
        private async UniTaskVoid Start(IFsm<IProcedureManager> procedureOwner)
        {
            await UniTask.NextFrame();
            
            // 编辑器下的模拟模式--直接进入获取App版本号流程
            if (AssetManager.Instance.PlayMode == EPlayMode.EditorSimulateMode)
            {
                ChangeState<ProcedureUpdateInitPackage>(procedureOwner);
                return;
            }

            // 单机离线模式--直接进入初始化YooAsset流程
            if (AssetManager.Instance.PlayMode == EPlayMode.OfflinePlayMode)
            {
                ChangeState<ProcedureUpdateInitPackage>(procedureOwner);
                return;
            }
            
            // TODO：这里应该看是否有游戏服务器，如果有则从游戏服务器获取获取相关全局信息再进行资源包的初始化，
            // TODO：如果没有，则直接请求资源服务器地址，获取上面的资源版本号。记录到GlobalConfigComponent中，然后再用这个版本号初始化资源包。
            // 热更模式--进入获取服务端全局信息流程
            ChangeState<ProcedureGetGlobalInfoFromServer>(procedureOwner);
        }
    }
}