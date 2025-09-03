using System;
using System.Linq;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Runtime
{
    /// <summary>
    /// 有限状态机管理器。
    /// 功能；
    /// 1. 管理多个有限状态机，包括创建、销毁、轮询等；
    /// </summary>
    public sealed class FsmManager : FuComponent
    {
        /// <summary>
        /// 有限状态机字典。key为状态机持有者类型和状态机名称的组合，value为有限状态机。
        /// </summary>
        private readonly Dictionary<TypeNamePair, Fsm> m_FsmDict = new();

        /// <summary>
        /// 临时有限状态机列表。用于在轮询中暂存有限状态机。
        /// </summary>
        private readonly List<Fsm> m_TempFsmList = new();

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
        /// 初始化
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            m_TempFsmList.Clear();
            if (m_FsmDict.Count <= 0) return;

            foreach (var fsm in m_FsmDict)
            {
                m_TempFsmList.Add(fsm.Value);
            }

            foreach (var fsm in m_TempFsmList.Where(fsm => !fsm.IsDestroyed))
            {
                fsm.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
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
        public bool HasFsm<T>() where T : class
        {
            return m_FsmDict.ContainsKey(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <param name="owner">有限状态机持有者类型。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(Type owner)
        {
            if (owner == null) throw new FuException("[FsmManager] 有限状态机持有者类型不能为空。");
            return m_FsmDict.ContainsKey(new TypeNamePair(owner));
        }

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm<T>(string fsmName) where T : class
        {
            return m_FsmDict.ContainsKey(new TypeNamePair(typeof(T), fsmName));
        }

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <param name="owner">有限状态机持有者类型。</param>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(Type owner, string fsmName)
        {
            if (owner == null) throw new FuException("[FsmManager] 有限状态机持有者类型不能为空。");
            return m_FsmDict.ContainsKey(new TypeNamePair(owner, fsmName));
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>要获取的有限状态机。</returns>
        public Fsm GetFsm<T>() where T : class
        {
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>要获取的有限状态机。</returns>
        public Fsm GetFsm(Type ownerType)
        {
            if (ownerType == null) throw new FuException("[FsmManager] 有限状态机持有者类型不能为空。");
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        public Fsm GetFsm<T>(string fsmName) where T : class
        {
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(typeof(T), fsmName));
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        public Fsm GetFsm(Type ownerType, string fsmName)
        {
            if (ownerType == null) throw new FuException("[FsmManager] 有限状态机持有者类型不能为空。");
            return m_FsmDict.GetValueOrDefault(new TypeNamePair(ownerType, fsmName));
        }

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <returns>所有有限状态机。</returns>
        public Fsm[] GetAllFsms()
        {
            var index   = 0;
            var results = new Fsm[m_FsmDict.Count];
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
        public void GetAllFsms(List<Fsm> results)
        {
            if (results == null) throw new FuException("[FsmManager] 结果列表不能为空。");
            results.Clear();
            foreach (var (_, fsmBase) in m_FsmDict)
            {
                results.Add(fsmBase);
            }
        }

        #endregion

        #region 创建有限状态机

        public Fsm CreateFsm<T>(T owner, params FsmStateBase[] states) where T : class
        {
            return CreateFsm(string.Empty, owner, states);
        }
        
        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public Fsm CreateFsm<T>(string fsmName, T owner, params FsmStateBase[] states) where T : class
        {
            var ownerType    = typeof(T);
            var typeNamePair = new TypeNamePair(ownerType, fsmName);
            
            if (HasFsm(ownerType)) 
                throw new FuException($"[FsmManager] 有限状态机 '{typeNamePair}' 已经存在，不能重复创建。");

            var fsm = Fsm.Create(fsmName, owner, states);
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
        public Fsm CreateFsm<T>(T owner, List<FsmStateBase> states) where T : class
        {
            return CreateFsm(string.Empty, owner, states);
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public Fsm CreateFsm<T>(string fsmName, T owner, List<FsmStateBase> states) where T : class
        {
            var typeNamePair = new TypeNamePair(typeof(T), fsmName);
            if (HasFsm<T>(fsmName))
                throw new FuException($"[FsmManager] 有限状态机 '{typeNamePair}' 已经存在，不能重复创建。");

            var fsm = Fsm.Create(fsmName, owner, states);
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
            if (ownerType == null) throw new FuException("[FsmManager] 有限状态机持有者类型不能为空。");
            return InternalDestroyFsm(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>(string fsmName) where T : class
        {
            return InternalDestroyFsm(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="fsmName">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Type ownerType, string fsmName)
        {
            if (ownerType == null) throw new FuException("[FsmManager] 有限状态机持有者类型不能为空。");
            return InternalDestroyFsm(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>(Fsm fsm) where T : class
        {
            if (fsm == null) throw new FuException("[FsmManager] 有限状态机不能为空。");
            return InternalDestroyFsm(new TypeNamePair(typeof(T), fsm.Name));
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Fsm fsm)
        {
            if (fsm == null) throw new FuException("[FsmManager] 有限状态机不能为空。");
            return InternalDestroyFsm(new TypeNamePair(fsm.Owner, fsm.Name));
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