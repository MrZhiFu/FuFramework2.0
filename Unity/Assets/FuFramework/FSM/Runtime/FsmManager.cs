using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Runtime
{
    /// <summary>
    /// 有限状态机管理器。
    /// </summary>
    public sealed class FsmManager : FuModule, IFsmManager
    {
        /// <summary>
        /// 有限状态机字典。key为有限状态机类型和名称的组合，value为有限状态机。
        /// </summary>
        private readonly Dictionary<TypeNamePair, FsmBase> m_FsmDict = new();

        /// <summary>
        /// 临时有限状态机列表。用于在轮询中暂存有限状态机。
        /// </summary>
        private readonly List<FsmBase> m_TempFsmList = new();

        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => 1;

        /// <summary>
        /// 获取有限状态机数量。
        /// </summary>
        public int Count => m_FsmDict.Count;

        /// <summary>
        /// 有限状态机管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_TempFsmList.Clear();
            if (m_FsmDict.Count <= 0) return;

            foreach (var fsm in m_FsmDict)
            {
                m_TempFsmList.Add(fsm.Value);
            }

            foreach (var fsm in m_TempFsmList)
            {
                if (fsm.IsDestroyed) continue;
                fsm.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理有限状态机管理器。
        /// </summary>
        protected override void Shutdown()
        {
            foreach (var fsm in m_FsmDict)
            {
                fsm.Value.Shutdown();
            }

            m_FsmDict.Clear();
            m_TempFsmList.Clear();
        }

        #region 获取有限状态机

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm<T>() where T : class => m_FsmDict.ContainsKey(new TypeNamePair(typeof(T)));

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(Type ownerType)
        {
            if (ownerType == null) throw new FuException("Owner type is invalid.");
            return m_FsmDict.ContainsKey(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm<T>(string name) where T : class
        {
            return m_FsmDict.ContainsKey(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(Type ownerType, string name)
        {
            if (ownerType == null) throw new FuException("Owner type is invalid.");
            return m_FsmDict.ContainsKey(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>要获取的有限状态机。</returns>
        public IFsm<T> GetFsm<T>() where T : class
        {
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(typeof(T))) as IFsm<T>;
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>要获取的有限状态机。</returns>
        public FsmBase GetFsm(Type ownerType)
        {
            if (ownerType == null) throw new FuException("Owner type is invalid.");
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        public IFsm<T> GetFsm<T>(string name) where T : class
        {
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(typeof(T), name)) as IFsm<T>;
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        public FsmBase GetFsm(Type ownerType, string name)
        {
            if (ownerType == null) throw new FuException("Owner type is invalid.");
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <returns>所有有限状态机。</returns>
        public FsmBase[] GetAllFsms()
        {
            var index = 0;
            var results = new FsmBase[m_FsmDict.Count];
            foreach (var (_, fsmBase) in m_FsmDict)
            {
                results[index++] = fsmBase;
            }

            return results;
        }

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <param name="results">所有有限状态机。</param>
        public void GetAllFsms(List<FsmBase> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            foreach (var (_, fsmBase) in m_FsmDict)
            {
                results.Add(fsmBase);
            }
        }

        #endregion

        #region 创建有限状态机

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public IFsm<T> CreateFsm<T>(T owner, params FsmStateBase<T>[] states) where T : class
        {
            return CreateFsm(string.Empty, owner, states);
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public IFsm<T> CreateFsm<T>(string name, T owner, params FsmStateBase<T>[] states) where T : class
        {
            var typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasFsm<T>(name))
                throw new FuException(Utility.Text.Format("Already exist FSM '{0}'.", typeNamePair));

            var fsm = Fsm<T>.Create(name, owner, states);
            m_FsmDict.Add(typeNamePair, fsm);
            return fsm;
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public IFsm<T> CreateFsm<T>(T owner, List<FsmStateBase<T>> states) where T : class
        {
            return CreateFsm(string.Empty, owner, states);
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public IFsm<T> CreateFsm<T>(string name, T owner, List<FsmStateBase<T>> states) where T : class
        {
            var typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasFsm<T>(name))
                throw new FuException(Utility.Text.Format("Already exist FSM '{0}'.", typeNamePair));

            var fsm = Fsm<T>.Create(name, owner, states);
            m_FsmDict.Add(typeNamePair, fsm);
            return fsm;
        }

        #endregion

        #region 销毁有限状态机

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>() where T : class => InternalDestroyFsm(new TypeNamePair(typeof(T)));

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Type ownerType)
        {
            if (ownerType == null) throw new FuException("Owner type is invalid.");
            return InternalDestroyFsm(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>(string name) where T : class
        {
            return InternalDestroyFsm(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="name">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Type ownerType, string name)
        {
            if (ownerType == null) throw new FuException("Owner type is invalid.");
            return InternalDestroyFsm(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>(IFsm<T> fsm) where T : class
        {
            if (fsm == null) throw new FuException("FSM is invalid.");
            return InternalDestroyFsm(new TypeNamePair(typeof(T), fsm.Name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(FsmBase fsm)
        {
            if (fsm == null) throw new FuException("FSM is invalid.");
            return InternalDestroyFsm(new TypeNamePair(fsm.OwnerType, fsm.Name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="typeNamePair"></param>
        /// <returns></returns>
        private bool InternalDestroyFsm(TypeNamePair typeNamePair)
        {
            if (!m_FsmDict.TryGetValue(typeNamePair, out var fsm)) return false;
            fsm.Shutdown();
            return m_FsmDict.Remove(typeNamePair);
        }

        #endregion
    }
}