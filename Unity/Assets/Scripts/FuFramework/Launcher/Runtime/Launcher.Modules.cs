using FuFramework.Core.Runtime;

using FuFramework.ReferencePool.Runtime;

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

        }
    }
}