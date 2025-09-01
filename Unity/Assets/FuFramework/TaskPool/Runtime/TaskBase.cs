using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.TaskPool.Runtime
{
    /// <summary>
    /// 任务基类。
    /// </summary>
    public abstract class TaskBase : IReference
    {
        /// 任务默认优先级。
        public const int DefaultPriority = 0;

        /// 任务的序列编号。
        public int SerialId { get; private set; }

        /// 任务的标签。
        public string Tag { get; private set; }

        /// 任务的优先级。
        public int Priority { get; private set; }

        /// 任务是否完成。
        public bool Done { get; set; }

        /// 任务描述。
        public virtual string Description => null;

        /// 任务的用户自定义数据。
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化任务基类。
        /// </summary>
        /// <param name="serialId">任务的序列编号。</param>
        /// <param name="tag">任务的标签。</param>
        /// <param name="priority">任务的优先级。</param>
        /// <param name="userData">任务的用户自定义数据。</param>
        public void Initialize(int serialId, string tag, int priority, object userData)
        {
            SerialId = serialId;
            Tag      = tag;
            Priority = priority;
            UserData = userData;
            Done     = false;
        }

        /// <summary>
        /// 清理任务基类。
        /// </summary>
        public virtual void Clear()
        {
            SerialId = 0;
            Tag      = null;
            Priority = DefaultPriority;
            Done     = false;
            UserData = null;
        }
    }
}