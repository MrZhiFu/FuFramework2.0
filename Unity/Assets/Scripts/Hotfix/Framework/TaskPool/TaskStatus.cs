// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.TaskPool
{
    /// <summary>
    /// 任务状态。
    /// </summary>
    public enum ETaskStatus : byte
    {
        /// <summary>
        /// 未开始。
        /// </summary>
        Todo = 0,

        /// <summary>
        /// 执行中。
        /// </summary>
        Doing,

        /// <summary>
        /// 完成。
        /// </summary>
        Done
    }
}
