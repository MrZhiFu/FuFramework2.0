using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 任务池。
    /// 实现原理：
    ///     主要是基于任务管理的模式，通过在Update中轮询来维护任务的代理状态，从而高效地调度和管理任务。以下是实现原理说明：
    ///
    /// m_FreeAgentStack：使用栈结构存放空闲的可用任务代理，以便快速获取和释放任务代理。栈的性质使得常见的“后进先出”操作易于实现，能够有效管理内存。
    /// m_WaitingTaskList：这是一个链表，用于存放正在等待执行的任务。这种设计允许灵活的添加和移除任务，同时保持任务的顺序。
    /// m_WorkingAgentList：同样是一个链表，用于存放当前正在工作的任务代理。可以用来监控哪些任务正在执行。
    ///
    /// 任务调度：TaskPool 通过管理这几个集合来调度任务。例如，当有新的任务需要执行时，从 m_FreeAgentStack 中取出一个任务代理，将其添加到 m_WorkingAgentList 中，同时将其添加到 m_WaitingTaskList 中直到其完成。
    /// 资源复用：通过使用栈来管理空闲的任务代理，TaskPool 可以有效地复用任务代理，减少了频繁分配和释放内存的开销，这在需要频繁创建和销毁任务的游戏环境中尤为重要。
    ///
    /// 运用举例：如下一些耗时的异步操作，都可以使用TaskPool来管理：
    /// 1.DownloadAgent：用于服务器文件的下载代理器
    /// 2.LoadResourceAgent：用于加载本地AssetBundle资源的加载器
    /// 3.WebRequestAgent：web请求代理
    /// </summary>
    /// <typeparam name="T">任务类型。</typeparam>
    public sealed class TaskPool<T> where T : TaskBase
    {
        /// 空闲的可用任务代理栈集合
        private readonly Stack<ITaskAgent<T>> m_FreeAgentStack;

        /// 等待中的任务链表集合
        private readonly FuLinkedList<T> m_WaitingTaskList;

        /// 工作中任务代理链表集合
        private readonly FuLinkedList<ITaskAgent<T>> m_WorkingAgentList;

        /// <summary>
        /// 批量移除时的复用任务快照缓冲。
        /// 移除会 ReferencePool.Recycle → task.Clear()（实现方/用户代码），其可能重入 AddTask/RemoveTask
        /// 修改本池容器；先快照并清空容器再逐个回收，避免「遍历中修改容器」。复用字段避免每次分配。
        /// </summary>
        private readonly List<T> m_TempTaskList = new();

        /// <summary>
        /// 批量移除时的复用代理快照缓冲（用途同 <see cref="m_TempTaskList"/>）。
        /// </summary>
        private readonly List<ITaskAgent<T>> m_TempAgentList = new();

        /// <summary>
        /// 初始化任务池的新实例。
        /// </summary>
        public TaskPool()
        {
            Paused = false;

            m_FreeAgentStack   = new Stack<ITaskAgent<T>>();
            m_WaitingTaskList  = new FuLinkedList<T>();
            m_WorkingAgentList = new FuLinkedList<ITaskAgent<T>>();
        }

        /// <summary>
        /// 回收任务的统一出口：包 try/catch 保证回收语义单调——
        /// ReferencePool.Recycle → task.Clear() 是用户代码，单个任务清理抛异常不应中断其余任务的回收。
        /// </summary>
        /// <param name="task">待回收的任务。</param>
        private static void SafeRecycle(T task)
        {
            if (task == null) return;

            try
            {
                ReferencePool.Recycle(task);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[TaskPool] 任务回收失败 (SerialId: {task.SerialId}): {e.Message}");
            }
        }

        /// <summary>
        /// 获取或设置任务池是否被暂停。
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// 获取任务代理总数量。
        /// </summary>
        public int TotalAgentCount => FreeAgentCount + WorkingAgentCount;

        /// <summary>
        /// 获取可用任务代理数量。
        /// </summary>
        public int FreeAgentCount => m_FreeAgentStack.Count;

        /// <summary>
        /// 获取工作中任务代理数量。
        /// </summary>
        public int WorkingAgentCount => m_WorkingAgentList.Count;

        /// <summary>
        /// 获取等待任务数量。
        /// </summary>
        public int WaitingTaskCount => m_WaitingTaskList.Count;

        /// <summary>
        /// 任务池轮询。
        /// </summary>
        /// <param name="deltaTime">逻辑帧间隔流逝时间，以秒为单位。</param>
        /// <param name="unscaledDeltaTime">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (Paused) return;

            _ProcessRunningTasks(deltaTime, unscaledDeltaTime); // 处理正在运行的任务
            _ProcessWaitingTasks();                             // 处理正在等待的任务
        }

        /// <summary>
        /// 关闭并清理任务池。
        /// </summary>
        public void Shutdown()
        {
            RemoveAllTasks();

            while (FreeAgentCount > 0)
            {
                m_FreeAgentStack.Pop().Shutdown();
            }
        }

        /// <summary>
        /// 增加任务代理。
        /// </summary>
        /// <param name="agent">要增加的任务代理。</param>
        public void AddAgent(ITaskAgent<T> agent)
        {
            if (agent == null) throw new InvalidOperationException("[TaskPool] 任务代理为空.");

            agent.Initialize();
            m_FreeAgentStack.Push(agent);
        }

        /// <summary>
        /// 根据任务的序列编号获取任务的信息。
        /// </summary>
        /// <param name="serialId">要获取信息的任务的序列编号。</param>
        /// <returns>任务的信息。</returns>
        public TaskInfo GetTaskInfo(int serialId)
        {
            foreach (var workingAgent in m_WorkingAgentList)
            {
                var workingTask = workingAgent.Task;
                if (workingTask.SerialId != serialId) continue;
                return new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority, workingTask.UserData, workingTask.Done ? ETaskStatus.Done : ETaskStatus.Doing,
                                    workingTask.Description);
            }

            foreach (var waitingTask in m_WaitingTaskList)
            {
                if (waitingTask.SerialId != serialId) continue;
                return new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority, waitingTask.UserData, ETaskStatus.Todo, waitingTask.Description);
            }

            return default;
        }

        /// <summary>
        /// 根据任务的标签获取任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的任务的标签。</param>
        /// <returns>任务的信息。</returns>
        public TaskInfo[] GetTaskInfos(string tag)
        {
            var results = new List<TaskInfo>();
            GetTaskInfos(tag, results);
            return results.ToArray();
        }

        /// <summary>
        /// 根据任务的标签获取任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的任务的标签。</param>
        /// <param name="results">任务的信息。</param>
        public void GetTaskInfos(string tag, List<TaskInfo> results)
        {
            if (results == null) throw new InvalidOperationException("[TaskPool] 结果列表为空.");

            results.Clear();

            foreach (var workingAgent in m_WorkingAgentList)
            {
                var workingTask = workingAgent.Task;
                if (workingTask.Tag != tag) continue;
                results.Add(new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority,
                                         workingTask.UserData, workingTask.Done ? ETaskStatus.Done : ETaskStatus.Doing, workingTask.Description));
            }

            foreach (var waitingTask in m_WaitingTaskList)
            {
                if (waitingTask.Tag != tag) continue;
                results.Add(new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority,
                                         waitingTask.UserData, ETaskStatus.Todo, waitingTask.Description));
            }
        }

        /// <summary>
        /// 获取所有任务的信息。
        /// </summary>
        /// <returns>所有任务的信息。</returns>
        public TaskInfo[] GetAllTaskInfos()
        {
            var index   = 0;
            var results = new TaskInfo[m_WorkingAgentList.Count + m_WaitingTaskList.Count];
            foreach (var workingAgent in m_WorkingAgentList)
            {
                var workingTask = workingAgent.Task;
                results[index++] = new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority,
                                                workingTask.UserData, workingTask.Done ? ETaskStatus.Done : ETaskStatus.Doing, workingTask.Description);
            }

            foreach (var waitingTask in m_WaitingTaskList)
            {
                results[index++] = new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority,
                                                waitingTask.UserData, ETaskStatus.Todo, waitingTask.Description);
            }

            return results;
        }

        /// <summary>
        /// 获取所有任务的信息。
        /// </summary>
        /// <param name="results">所有任务的信息。</param>
        public void GetAllTaskInfos(List<TaskInfo> results)
        {
            if (results == null) throw new InvalidOperationException("[TaskPool] 结果列表为空.");

            results.Clear();

            foreach (var workingAgent in m_WorkingAgentList)
            {
                var workingTask = workingAgent.Task;
                results.Add(new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority,
                                         workingTask.UserData, workingTask.Done ? ETaskStatus.Done : ETaskStatus.Doing, workingTask.Description));
            }

            foreach (var waitingTask in m_WaitingTaskList)
            {
                results.Add(new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority,
                                         waitingTask.UserData, ETaskStatus.Todo, waitingTask.Description));
            }
        }

        /// <summary>
        /// 增加任务。
        /// </summary>
        /// <param name="task">要增加的任务。</param>
        public void AddTask(T task)
        {
            var current = m_WaitingTaskList.Last;
            while (current != null)
            {
                if (task.Priority <= current.Value.Priority) break;
                current = current.Previous;
            }

            if (current != null)
                m_WaitingTaskList.AddAfter(current, task);
            else
                m_WaitingTaskList.AddFirst(task);
        }

        /// <summary>
        /// 根据任务的序列编号移除任务。
        /// </summary>
        /// <param name="serialId">要移除任务的序列编号。</param>
        /// <returns>是否移除任务成功。</returns>
        public bool RemoveTask(int serialId)
        {
            // 手工结点遍历（不用 foreach 枚举器）：回收用户代码可能重入修改容器，枚举器会失效。
            // 先「摘链」再「回收」：摘链后本方法不再触碰容器，重入的增删不会与本方法交叉。
            for (var node = m_WaitingTaskList.First; node != null; node = node.Next)
            {
                var task = node.Value;
                if (task.SerialId != serialId) continue;
                m_WaitingTaskList.Remove(node);
                SafeRecycle(task);
                return true;
            }

            var currentWorkingAgent = m_WorkingAgentList.First;
            while (currentWorkingAgent != null)
            {
                var next         = currentWorkingAgent.Next;
                var workingAgent = currentWorkingAgent.Value;
                var task         = workingAgent.Task;

                if (task.SerialId == serialId)
                {
                    workingAgent.Reset();
                    m_FreeAgentStack.Push(workingAgent);
                    m_WorkingAgentList.Remove(currentWorkingAgent);
                    SafeRecycle(task);
                    return true;
                }

                currentWorkingAgent = next;
            }

            return false;
        }

        /// <summary>
        /// 根据任务的标签移除任务。
        /// </summary>
        /// <param name="tag">要移除任务的标签。</param>
        /// <returns>移除任务的数量。</returns>
        public int RemoveTasks(string tag)
        {
            var count = 0;

            var currentWaitingTask = m_WaitingTaskList.First;
            while (currentWaitingTask != null)
            {
                var next = currentWaitingTask.Next;
                var task = currentWaitingTask.Value;
                if (task.Tag == tag)
                {
                    m_WaitingTaskList.Remove(currentWaitingTask);
                    SafeRecycle(task);
                    count++;
                }

                currentWaitingTask = next;
            }

            var currentWorkingAgent = m_WorkingAgentList.First;
            while (currentWorkingAgent != null)
            {
                var next         = currentWorkingAgent.Next;
                var workingAgent = currentWorkingAgent.Value;
                var task         = workingAgent.Task;
                if (task.Tag == tag)
                {
                    workingAgent.Reset();
                    m_FreeAgentStack.Push(workingAgent);
                    m_WorkingAgentList.Remove(currentWorkingAgent);
                    SafeRecycle(task);
                    count++;
                }

                currentWorkingAgent = next;
            }

            return count;
        }

        /// <summary>
        /// 移除所有任务。
        /// </summary>
        /// <returns>移除任务的数量。</returns>
        public int RemoveAllTasks()
        {
            var count = m_WaitingTaskList.Count + m_WorkingAgentList.Count;
            if (count == 0) return 0;

            // 先快照并清空容器，再逐个回收：ReferencePool.Recycle → task.Clear() 是实现方(用户)代码，
            // 可能经同步延续重入 AddTask/RemoveTask 修改容器；边遍历边回收会「遍历中修改容器」，
            // 且首个任务抛异常会中断其余任务与代理的回收（回收语义不单调）。
            m_TempTaskList.Clear();
            for (var node = m_WaitingTaskList.First; node != null; node = node.Next)
            {
                m_TempTaskList.Add(node.Value);
            }

            m_WaitingTaskList.Clear();

            m_TempAgentList.Clear();
            for (var node = m_WorkingAgentList.First; node != null; node = node.Next)
            {
                m_TempAgentList.Add(node.Value);
            }

            m_WorkingAgentList.Clear();

            for (var i = 0; i < m_TempTaskList.Count; i++)
            {
                SafeRecycle(m_TempTaskList[i]);
            }

            m_TempTaskList.Clear();

            for (var i = 0; i < m_TempAgentList.Count; i++)
            {
                var workingAgent = m_TempAgentList[i];
                // 先取任务再 Reset(Reset 会清空 agent.Task)，随后归还空闲栈
                var task = workingAgent.Task;
                workingAgent.Reset();
                m_FreeAgentStack.Push(workingAgent);
                SafeRecycle(task);
            }

            m_TempAgentList.Clear();

            return count;
        }

        /// <summary>
        /// 处理正在运行的任务
        /// </summary>
        /// <param name="deltaTime">逻辑帧间隔流逝时间，以秒为单位。</param>
        /// <param name="unscaledDeltaTime">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
        private void _ProcessRunningTasks(float deltaTime, float unscaledDeltaTime)
        {
            var current = m_WorkingAgentList.First;

            // current.List != null 表示该结点仍挂在工作链表中：Update/Reset 等实现方(用户)代码可能同步经
            // RemoveTask/RemoveTasks/RemoveAllTasks 摘除结点（_ReleaseNode 会把 Value 置空并回缓存复用），
            // 此后 current.Value 为 null 或已指向其它代理，继续取 .Task 会 NRE 并逃逸到无保护的
            // ModuleManager.Update（整帧中断）。判据与 _ProcessWaitingTasks 的「结点仍挂在链表中」口径一致。
            while (current != null && current.List != null)
            {
                var agent = current.Value;
                var task  = agent?.Task;

                // 先缓存 next 再调用用户代码：Update/Reset 内部可能经同步延续调用 RemoveTask/RemoveTasks
                // 摘除本结点（结点会被回收复用），之后 current.Next 会读到失效/复用的结点，导致本帧后续代理被静默跳过。
                // 与 _ProcessWaitingTasks 的「先缓存 next 再调用户代码」保持一致。
                var next = current.Next;

                // 代理已被同步 Reset（Task 置空）而其结点尚未摘链：本帧跳过，避免 task.Done 的空引用。
                if (task == null)
                {
                    current = next;
                    continue;
                }

                if (!task.Done)
                {
                    agent.Update(deltaTime, unscaledDeltaTime);
                    current = next;
                    continue;
                }

                agent.Reset();
                m_FreeAgentStack.Push(agent);
                m_WorkingAgentList.Remove(current);
                SafeRecycle(task);
                current = next;
            }
        }

        /// <summary>
        /// 处理正在等待的任务
        /// </summary>
        private void _ProcessWaitingTasks()
        {
            var current = m_WaitingTaskList.First;

            // current.List != null 表示该结点仍挂在等待链表中；若迭代途中结点被同步归还(见下方说明)而摘链，则直接结束本轮推进。
            while (current != null && current.List != null && FreeAgentCount > 0)
            {
                var agent     = m_FreeAgentStack.Pop();
                var agentNode = m_WorkingAgentList.AddLast(agent);
                var task      = current.Value;
                var next      = current.Next;
                var status    = agent.Start(task);

                // agent.Start 是实现方(用户)代码，可能在返回前经同步延续调用 RemoveTask/RemoveTasks/RemoveAllTasks
                // 把本任务摘链并 ReferencePool.Recycle。此时任务的所有权已归还，不再是本池本次推进的对象：
                // 若仍按下面的逻辑处理，会对已回收的任务二次 Recycle(抛「该对象已经被释放」)，并对已摘链的结点重复 Remove(抛异常)，
                // 且异常会逃逸到无保护的 ModuleManager.Update。
                // 以「任务是否仍挂在等待链表中」为唯一所有权判据，保证谁摘链谁回收，且至多回收一次。
                // 判据用 O(1) 的结点状态而非 Contains(task)(O(n²))：结点仍在链表中 且 仍持有本任务——
                // 后者排除「结点被摘链回收后又被 AddTask 复用给别的任务」这一误判（此时 List != null 但 Value 已换人）。
                if (current.List == null || !ReferenceEquals(current.Value, task))
                {
                    // 任务已被 Start 内部同步归还：归还本次临时占用的工作代理(若它尚未被一并归还)，避免代理泄漏。
                    if (agentNode.List != null)
                    {
                        agent.Reset();
                        m_FreeAgentStack.Push(agent);
                        m_WorkingAgentList.Remove(agentNode);
                    }

                    current = next;
                    continue;
                }

                if (status is EStartTaskStatus.Done or EStartTaskStatus.HasToWait or EStartTaskStatus.UnknownError)
                {
                    agent.Reset();
                    // agent.Reset 也是用户代码，可能同步归还本次代理(摘链)；结点仍在链表中才归还，
                    // 避免重复压入空闲栈或对已摘链结点 Remove(抛异常)。
                    if (agentNode.List != null)
                    {
                        m_FreeAgentStack.Push(agent);
                        m_WorkingAgentList.Remove(agentNode);
                    }
                }

                var shouldRemoveWaiting = status is EStartTaskStatus.Done or EStartTaskStatus.CanResume or EStartTaskStatus.UnknownError;

                // 结点仍挂在等待链表中才摘链：上面的 Reset 路径是用户代码，可能已把它摘除，
                // 对已摘链结点 Remove 会抛 InvalidOperationException 并逃逸到无保护的 ModuleManager.Update。
                if (shouldRemoveWaiting && current.List != null)
                    m_WaitingTaskList.Remove(current);

                if (status is EStartTaskStatus.Done or EStartTaskStatus.UnknownError)
                    SafeRecycle(task);

                current = next;
            }
        }
    }
}