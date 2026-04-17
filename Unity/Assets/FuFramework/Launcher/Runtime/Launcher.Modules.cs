using FuFramework.Asset.Runtime;
using FuFramework.Config.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Coroutine.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Entity.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Guide.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.Model.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.RedDot.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.SaveData.Runtime;
using FuFramework.Scene.Runtime;
using FuFramework.Sound.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Web.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// Launcher 模块注册部分
    /// </summary>
    public partial class Launcher
    {
        /// <summary>
        /// 注册框架各个模块
        /// 注意：注册顺序不可修改，防止某些模块依赖于其他模块时出错。
        /// </summary>
        private void RegisterModules()
        {
            ModuleManager.RegisterModule<ReferencePoolModule>(); // 引用池管理模块
            ModuleManager.RegisterModule<ObjectPoolModule>();    // 对象池管理模块
            ModuleManager.RegisterModule<FsmModule>();           // 有限状态机管理模块
            ModuleManager.RegisterModule<ProcedureModule>();     // 流程管理模块
            ModuleManager.RegisterModule<EventModule>();         // 事件管理模块
            ModuleManager.RegisterModule<CoroutineModule>();     // 协程管理模块
            ModuleManager.RegisterModule<MonoModule>();          // Mono管理模块
            ModuleManager.RegisterModule<TimerModule>();         // 计时器管理模块
            ModuleManager.RegisterModule<AssetModule>();         // 资源管理模块
            ModuleManager.RegisterModule<DownloadModule>();      // 下载管理模块
            ModuleManager.RegisterModule<DataSaveModule>();      // 本地存储数据管理模块

            ModuleManager.RegisterModule<ConfigModule>();       // 配置管理模块
            ModuleManager.RegisterModule<SceneModule>();        // 场景管理模块
            ModuleManager.RegisterModule<SoundModule>();        // 声音管理模块
            ModuleManager.RegisterModule<EntityModule>();       // 实体管理模块
            ModuleManager.RegisterModule<NetworkModule>();      // 网络管理模块
            ModuleManager.RegisterModule<UIModule>();           // UI管理模块
            ModuleManager.RegisterModule<GuideModule>();        // 红点管理模块
            ModuleManager.RegisterModule<RedDotModule>();       // 引导管理模块
            ModuleManager.RegisterModule<LocalizationModule>(); // 本地化管理模块
            ModuleManager.RegisterModule<ModelModule>();        // 数据模型管理模块
            ModuleManager.RegisterModule<WebModule>();          // Web管理模块
        }
    }
}