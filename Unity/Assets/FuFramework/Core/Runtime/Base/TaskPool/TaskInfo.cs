using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 任务信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct TaskInfo
    {
        /// 任务的标签
        private readonly string m_Tag;

        /// 任务的序列编号
        private readonly int m_SerialId;

        /// 任务的优先级
        private readonly int m_Priority;

        /// 任务信息是否有效
        private readonly bool m_IsValid;

        /// 任务的用户自定义数据
        private readonly object m_UserData;

        /// 任务描述
        private readonly string m_Description;

        /// 任务状态
        private readonly TaskStatus m_Status;

        /// <summary>
        /// 初始化任务信息的新实例。
        /// </summary>
        /// <param name="serialId">任务的序列编号。</param>
        /// <param name="tag">任务的标签。</param>
        /// <param name="priority">任务的优先级。</param>
        /// <param name="userData">任务的用户自定义数据。</param>
        /// <param name="status">任务状态。</param>
        /// <param name="description">任务描述。</param>
        public TaskInfo(int serialId, string tag, int priority, object userData, TaskStatus status, string description)
        {
            m_Tag         = tag;
            m_IsValid     = true;
            m_Status      = status;
            m_SerialId    = serialId;
            m_Priority    = priority;
            m_UserData    = userData;
            m_Description = description;
        }

        /// <summary>
        /// 获取任务信息是否有效。
        /// </summary>
        public bool IsValid => m_IsValid;

        /// <summary>
        /// 获取任务的序列编号。
        /// </summary>
        public int SerialId => m_IsValid ? m_SerialId : throw new FuException("Data is invalid.");

        /// <summary>
        /// 获取任务的标签。
        /// </summary>
        public string Tag => m_IsValid ? m_Tag : throw new FuException("Data is invalid.");

        /// <summary>
        /// 获取任务的优先级。
        /// </summary>
        public int Priority => m_IsValid ? m_Priority : throw new FuException("Data is invalid.");

        /// <summary>
        /// 获取任务的用户自定义数据。
        /// </summary>
        public object UserData => m_IsValid ? m_UserData : throw new FuException("Data is invalid.");

        /// <summary>
        /// 获取任务状态。
        /// </summary>
        public TaskStatus Status => m_IsValid ? m_Status : throw new FuException("Data is invalid.");

        /// <summary>
        /// 获取任务描述。
        /// </summary>
        public string Description => m_IsValid ? m_Description : throw new FuException("Data is invalid.");
    }
}
