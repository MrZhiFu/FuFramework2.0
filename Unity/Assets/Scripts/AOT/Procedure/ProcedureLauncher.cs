using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.UI.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using Launcher.UI;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 入口启动流程。
    /// 功能：
    ///     1. 设置FairyGUI的Loader加载器为自定义的加载器。
    ///     2. 绑定Fui自定义组件。
    ///     3. 启动热更进度显示UI。
    ///     4. 单机模式：进入获取全局信息流程。
    ///     5. 热更模式：进入获取服务端全局信息流程。
    /// </summary>
    public class ProcedureLauncher : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 1;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入首次启动流程------</color>");

            // 设置FairyGUI的Loader加载器为自定义加载器
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));

            // 绑定自动生成的Fui自定义组件(AOT下)
            BindCustomComps();

            // 启动流程
            Start().Forget();
        }

        //@formatter:off
        /// <summary>
        /// 绑定Fui自定义组件.
        /// 方法体由FUI导出时自动生成,请勿修改.
        /// 特殊:如果清理了FUI包里的所有自定义组件,且清理了它们的绑定代码,则可以删除对应的BindAll()调用.
        /// </summary>
        private static void BindCustomComps()
        {
        }
        //@formatter:on

        /// <summary>
        /// 进入获取全局信息流程
        /// </summary>
        private async UniTaskVoid Start()
        {
            await UniTask.NextFrame();

            // 启动热更进度UI
            await LauncherUIHelper.Start();

            // 编辑器下的模拟模式/单机离线模式--进入初始化资源包流程
            if (GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode)
            {
                ChangeState<ProcedureInitPackage>();
                return;
            }

            // 热更模式--进入获取远端资源更新配置流程
            ChangeState<ProcedureReqRemoteUpdateConfig>();
        }
    }
}