using FuFramework.Core.Runtime;
using GameFrameX.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 打开界面成功事件。
    /// </summary>
    public sealed class OpenUISuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 打开界面成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OpenUISuccessEventArgs).FullName;

        /// <summary>
        /// 获取打开界面成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public ViewBase ViewBase { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public OpenUISuccessEventArgs()
        {
            ViewBase = null;
            UserData = null;
        }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="view">打开成功的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static OpenUISuccessEventArgs Create(ViewBase view,object userData)
        {
            var openUISuccessEventArgs = ReferencePool.Acquire<OpenUISuccessEventArgs>();
            openUISuccessEventArgs.ViewBase = view;
            openUISuccessEventArgs.UserData = userData;
            return openUISuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            ViewBase = null;
            UserData = null;
        }
    }
}