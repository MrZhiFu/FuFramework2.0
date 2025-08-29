using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Runtime
{
    /// <summary>
    /// 有限状态机组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/FSM")]
    public sealed class FsmComponent : FuComponent
    {
        /// <summary>
        /// 有限状态机管理器。
        /// </summary>
        private IFsmManager m_FsmManager;

        /// <summary>
        /// 获取有限状态机数量。
        /// </summary>
        public int Count => m_FsmManager.Count;

        protected override void OnInit()
        {
            m_FsmManager = FuEntry.GetModule<IFsmManager>();
            if (m_FsmManager == null) Log.Fatal("FSM manager is invalid.");
        }
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }
        protected override void OnShutdown(ShutdownType shutdownType)
        {
        }
        
        #region 获取有限状态机

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm<T>() where T : class => m_FsmManager.HasFsm<T>();

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(Type ownerType) => m_FsmManager.HasFsm(ownerType);

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm<T>(string fsmName) where T : class => m_FsmManager.HasFsm<T>(fsmName);

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(Type ownerType, string fsmName) => m_FsmManager.HasFsm(ownerType, fsmName);

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>要获取的有限状态机。</returns>
        public IFsm<T> GetFsm<T>() where T : class => m_FsmManager.GetFsm<T>();

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>要获取的有限状态机。</returns>
        public FsmBase GetFsm(Type ownerType) => m_FsmManager.GetFsm(ownerType);

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        public IFsm<T> GetFsm<T>(string fsmName) where T : class => m_FsmManager.GetFsm<T>(fsmName);

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        public FsmBase GetFsm(Type ownerType, string fsmName) => m_FsmManager.GetFsm(ownerType, fsmName);

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        public FsmBase[] GetAllFsmList() => m_FsmManager.GetAllFsms();

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <param name="results">所有有限状态机。</param>
        public void GetAllFsmList(List<FsmBase> results) => m_FsmManager.GetAllFsms(results);

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
            return m_FsmManager.CreateFsm(owner, states);
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public IFsm<T> CreateFsm<T>(string fsmName, T owner, params FsmStateBase<T>[] states) where T : class
        {
            return m_FsmManager.CreateFsm(fsmName, owner, states);
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
            return m_FsmManager.CreateFsm(owner, states);
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public IFsm<T> CreateFsm<T>(string fsmName, T owner, List<FsmStateBase<T>> states) where T : class
        {
            return m_FsmManager.CreateFsm(fsmName, owner, states);
        }

        #endregion

        #region 销毁有限状态机

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>() where T : class => m_FsmManager.DestroyFsm<T>();

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Type ownerType) => m_FsmManager.DestroyFsm(ownerType);

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsmName">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>(string fsmName) where T : class => m_FsmManager.DestroyFsm<T>(fsmName);

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="fsmName">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Type ownerType, string fsmName) => m_FsmManager.DestroyFsm(ownerType, fsmName);

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm<T>(IFsm<T> fsm) where T : class => m_FsmManager.DestroyFsm(fsm);

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(FsmBase fsm) => m_FsmManager.DestroyFsm(fsm);

        #endregion
    }
}