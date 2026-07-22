using System;

// ReSharper disable once CheckNamespace

namespace AOT.Launch
{
    /// <summary>
    /// 启动热更资源加载界面句柄接口。
    /// 功能： 由 AOT 的 LaunchView 实现，AOT 启动流程与 HotfixLauncher 均面向此接口编程，
    ///       使加载界面能跨越「下载(AOT)→加载配置/UI(热更)」两阶段并在登录界面打开时关闭。
    /// </summary>
    public interface ILaunchView
    {
        /// <summary>
        /// 设置热更资源加载提示文本。
        /// </summary>
        /// <param name="text">提示文本。</param>
        void SetTip(string text);

        /// <summary>
        /// 设置热更资源加载进度与提示。
        /// </summary>
        /// <param name="progress">下载进度(0~1)。</param>
        /// <param name="text">提示文本。</param>
        void SetProgress(float progress, string text);

        /// <summary>
        /// 设置是否显示更新确认框。
        /// </summary>
        /// <param name="need">是否显示更新确认框。</param>
        void SetNeedUpgrade(bool need);

        /// <summary>
        /// 设置是否处于下载中状态。
        /// </summary>
        /// <param name="downloading">是否处于下载中状态。</param>
        void SetDownloading(bool downloading);

        /// <summary>
        /// 显示更新确认框。
        /// </summary>
        /// <param name="content">更新内容。</param>
        /// <param name="onConfirm">确认回调。</param>
        void ShowUpdateDialog(string content, Action onConfirm);

        /// <summary>
        /// 关闭热更资源加载界面。
        /// </summary>
        void Close();
    }
}