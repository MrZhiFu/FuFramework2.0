using System;
using System.Collections.Generic;
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
            foreach (var task in m_WaitingTaskList)
            {
                if (task.SerialId != serialId) continue;
                m_WaitingTaskList.Remove(task);
                ReferencePool.Recycle(task);
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
                    ReferencePool.Recycle(task);
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
                    ReferencePool.Recycle(task);
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
                    ReferencePool.Recycle(task);
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

            foreach (var task in m_WaitingTaskList)
            {
                ReferencePool.Recycle(task);
            }

            m_WaitingTaskList.Clear();

            foreach (var workingAgent in m_WorkingAgentList)
            {
                var task = workingAgent.Task;
                workingAgent.Reset();
                m_FreeAgentStack.Push(workingAgent);
                ReferencePool.Recycle(task);
            }

            m_WorkingAgentList.Clear();

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
            while (current != null)
            {
                var task = current.Value.Task;
                if (!task.Done)
                {
                    current.Value.Update(deltaTime, unscaledDeltaTime);
                    current = current.Next;
                    continue;
                }

                var next = current.Next;
                current.Value.Reset();
                m_FreeAgentStack.Push(current.Value);
                m_WorkingAgentList.Remove(current);
                ReferencePool.Recycle(task);
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
                if (!m_WaitingTaskList.Contains(task))
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
                    m_FreeAgentStack.Push(agent);
                    m_WorkingAgentList.Remove(agentNode);
                }

                if (status is EStartTaskStatus.Done or EStartTaskStatus.CanResume or EStartTaskStatus.UnknownError)
                    m_WaitingTaskList.Remove(current);

                if (status is EStartTaskStatus.Done or EStartTaskStatus.UnknownError)
                    ReferencePool.Recycle(task);

                current = next;
            }
        }
    }
}