// ReSharper disable once CheckNamespace
namespace FuFramework.TaskPool.Runtime
{
    /// <summary>
    /// 开始处理任务的状态。
    /// </summary>
    public enum StartTaskStatus : byte
    {
        /// <summary>
        /// 完成此任务。
        /// </summary>
        Done = 0,

        /// <summary>
        /// 恢复处理此任务。
        /// </summary>
        CanResume,

        /// <summary>
        /// 不能处理此任务，需等待。
        /// </summary>
        HasToWait,

        /// <summary>
        /// 不能处理此任务，出现未知错误。
        /// </summary>
        UnknownError
    }
}