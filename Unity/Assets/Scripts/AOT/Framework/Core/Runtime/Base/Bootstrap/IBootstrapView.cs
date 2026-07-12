// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 启动加载界面句柄接口。
    /// 由 AOT 的 BootstrapView 实现，经 HotfixLauncher.MainAsync 传入热更侧，
    /// 使加载界面能跨越「下载(AOT)→加载配置/UI(热更)」两阶段并在登录界面打开时关闭。
    /// </summary>
    public interface IBootstrapView
    {
        /// <summary>
        /// 设置加载提示文本。
        /// </summary>
        void SetTip(string text);

        /// <summary>
        /// 关闭加载界面。
        /// </summary>
        void Close();
    }
}
