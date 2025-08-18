using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Runtime
{
    /// <summary>
    /// 有限状态机。
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型。</typeparam>
    internal sealed class Fsm<T> : FsmBase, IReference, IFsm<T> where T : class
    {
        /// <summary>
        /// 记录该有限状态机的所有状态的字典。key为状态类型，value为状态实例。
        /// </summary>
        private readonly Dictionary<Type, FsmStateBase<T>> m_StateDict = new();

        /// <summary>
        /// 记录该有限状态机的所有数据变量的字典。key为变量名，value为变量实例。
        /// </summary>
        private Dictionary<string, Variable> m_DataDict;

        /// <summary>
        /// 该有限状态机当前状态持续时间。
        /// </summary>
        private float m_CurrentStateTime;

        /// <summary>
        /// 该有限状态机是否被销毁。
        /// </summary>
        private bool m_IsDestroyed = true;


        /// <summary>
        /// 获取有限状态机持有者。
        /// </summary>
        public T Owner { get; private set; }

        /// <summary>
        /// 获取当前有限状态机状态。
        /// </summary>
        public FsmStateBase<T> CurrentStateBase { get; private set; }


        /// <summary>
        /// 获取有限状态机持有者类型。
        /// </summary>
        public override Type OwnerType => typeof(T);

        /// <summary>
        /// 获取有限状态机中状态的数量。
        /// </summary>
        public override int FsmStateCount => m_StateDict.Count;

        /// <summary>
        /// 获取有限状态机是否正在运行。
        /// </summary>
        public override bool IsRunning => CurrentStateBase != null;

        /// <summary>
        /// 获取有限状态机是否被销毁。
        /// </summary>
        public override bool IsDestroyed => m_IsDestroyed;

        /// <summary>
        /// 获取当前有限状态机状态名称。
        /// </summary>
        public override string CurrentStateName => CurrentStateBase?.GetType().FullName;

        /// <summary>
        /// 获取当前有限状态机状态持续时间。
        /// </summary>
        public override float CurrentStateTime => m_CurrentStateTime;


        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm<T> Create(string name, T owner, params FsmStateBase<T>[] states)
        {
            if (owner == null) throw new FuException("FSM owner is invalid.");
            if (states == null || states.Length < 1) throw new FuException("FSM states is invalid.");

            var fsm = ReferencePool.Acquire<Fsm<T>>();
            fsm.Name = name;
            fsm.Owner = owner;
            fsm.m_IsDestroyed = false;

            foreach (var state in states)
            {
                if (state == null) throw new FuException("FSM states is invalid.");
                var stateType = state.GetType();
                if (!fsm.m_StateDict.TryAdd(stateType, state))
                {
                    throw new FuException(Utility.Text.Format("FSM '{0}' state '{1}' is already exist.", new TypeNamePair(typeof(T), name),
                        stateType.FullName));
                }

                state.OnInit(fsm);
            }

            return fsm;
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm<T> Create(string name, T owner, List<FsmStateBase<T>> states)
        {
            if (owner == null) throw new FuException("FSM owner is invalid.");
            if (states == null || states.Count < 1) throw new FuException("FSM states is invalid.");

            var fsm = ReferencePool.Acquire<Fsm<T>>();
            fsm.Name = name;
            fsm.Owner = owner;
            fsm.m_IsDestroyed = false;

            foreach (var state in states)
            {
                if (state == null) throw new FuException("FSM states is invalid.");
                var stateType = state.GetType();
                if (!fsm.m_StateDict.TryAdd(stateType, state))
                {
                    throw new FuException(Utility.Text.Format("FSM '{0}' state '{1}' is already exist.", new TypeNamePair(typeof(T), name),
                        stateType.FullName));
                }

                state.OnInit(fsm);
            }

            return fsm;
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <typeparam name="TState">要开始的有限状态机状态类型。</typeparam>
        public void Start<TState>() where TState : FsmStateBase<T>
        {
            if (IsRunning) throw new FuException("FSM is running, can not start again.");

            FsmStateBase<T> stateBase = GetState<TState>();
            if (stateBase == null)
            {
                throw new FuException(Utility.Text.Format("FSM '{0}' can not start state '{1}' which is not exist.",
                    new TypeNamePair(typeof(T), Name), typeof(TState).FullName));
            }

            m_CurrentStateTime = 0f;
            CurrentStateBase = stateBase;
            CurrentStateBase.OnEnter(this);
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <param name="stateType">要开始的有限状态机状态类型。</param>
        public void Start(Type stateType)
        {
            if (IsRunning) throw new FuException("FSM is running, can not start again.");
            if (stateType == null) throw new FuException("State type is invalid.");

            if (!typeof(FsmStateBase<T>).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));

            var state = GetState(stateType);
            if (state == null)
            {
                throw new FuException(Utility.Text.Format("FSM '{0}' can not start state '{1}' which is not exist.",
                    new TypeNamePair(typeof(T), Name), stateType.FullName));
            }

            m_CurrentStateTime = 0f;
            CurrentStateBase = state;
            CurrentStateBase.OnEnter(this);
        }

        /// <summary>
        /// 有限状态机轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (CurrentStateBase == null) return;
            m_CurrentStateTime += elapseSeconds;
            CurrentStateBase.OnUpdate(this, elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 清理有限状态机。
        /// </summary>
        public void Clear()
        {
            CurrentStateBase?.OnLeave(this, true);

            foreach (var (_, state) in m_StateDict)
            {
                state.OnDestroy(this);
            }

            Name = null;
            Owner = null;
            m_StateDict.Clear();

            if (m_DataDict != null)
            {
                foreach (var (_, data) in m_DataDict)
                {
                    if (data == null) continue;
                    ReferencePool.Release(data);
                }

                m_DataDict.Clear();
            }

            CurrentStateBase = null;
            m_CurrentStateTime = 0f;
            m_IsDestroyed = true;
        }

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要检查的有限状态机状态类型。</typeparam>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState<TState>() where TState : FsmStateBase<T> => m_StateDict.ContainsKey(typeof(TState));

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <param name="stateType">要检查的有限状态机状态类型。</param>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState(Type stateType)
        {
            if (stateType == null) throw new FuException("State type is invalid.");
            if (!typeof(FsmStateBase<T>).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            return m_StateDict.ContainsKey(stateType);
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要获取的有限状态机状态类型。</typeparam>
        /// <returns>要获取的有限状态机状态。</returns>
        public TState GetState<TState>() where TState : FsmStateBase<T>
        {
            return m_StateDict.TryGetValue(typeof(TState), out var state) ? state as TState : null;
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <param name="stateType">要获取的有限状态机状态类型。</param>
        /// <returns>要获取的有限状态机状态。</returns>
        public FsmStateBase<T> GetState(Type stateType)
        {
            if (stateType == null) throw new FuException("State type is invalid.");
            if (!typeof(FsmStateBase<T>).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            return m_StateDict.GetValueOrDefault(stateType);
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态。</returns>
        public FsmStateBase<T>[] GetAllStates()
        {
            var index = 0;
            var results = new FsmStateBase<T>[m_StateDict.Count];
            foreach (var state in m_StateDict)
            {
                results[index++] = state.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <param name="results">有限状态机的所有状态。</param>
        public void GetAllStates(List<FsmStateBase<T>> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            foreach (var (_, state) in m_StateDict)
            {
                results.Add(state);
            }
        }

        /// <summary>
        /// 是否存在有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>有限状态机数据是否存在。</returns>
        public bool HasData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new FuException("Data name is invalid.");
            return m_DataDict != null && m_DataDict.ContainsKey(name);
        }

        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <typeparam name="TData">要获取的有限状态机数据的类型。</typeparam>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        public TData GetData<TData>(string name) where TData : Variable => GetData(name) as TData;

        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        public Variable GetData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new FuException("Data name is invalid.");
            return m_DataDict?.GetValueOrDefault(name);
        }

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <typeparam name="TData">要设置的有限状态机数据的类型。</typeparam>
        /// <param name="name">有限状态机数据名称。</param>
        /// <param name="data">要设置的有限状态机数据。</param>
        public void SetData<TData>(string name, TData data) where TData : Variable
        {
            SetData(name, data as Variable);
        }

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <param name="data">要设置的有限状态机数据。</param>
        public void SetData(string name, Variable data)
        {
            if (string.IsNullOrEmpty(name)) throw new FuException("Data name is invalid.");

            m_DataDict ??= new Dictionary<string, Variable>(StringComparer.Ordinal);

            var oldData = GetData(name);
            if (oldData != null)
                ReferencePool.Release(oldData);

            m_DataDict[name] = data;
        }

        /// <summary>
        /// 移除有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>是否移除有限状态机数据成功。</returns>
        public bool RemoveData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new FuException("Data name is invalid.");
            if (m_DataDict == null) return false;

            var oldData = GetData(name);
            if (oldData != null) ReferencePool.Release(oldData);
            return m_DataDict.Remove(name);
        }

        /// <summary>
        /// 关闭并清理有限状态机。
        /// </summary>
        internal override void Shutdown() => ReferencePool.Release(this);

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
        internal void ChangeState<TState>() where TState : FsmStateBase<T> => ChangeState(typeof(TState));

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="stateType">要切换到的有限状态机状态类型。</param>
        internal void ChangeState(Type stateType)
        {
            if (CurrentStateBase == null) throw new FuException("Current state is invalid.");
            var state = GetState(stateType);
            if (state == null)
            {
                throw new FuException(Utility.Text.Format("FSM '{0}' can not change state to '{1}' which is not exist.",
                    new TypeNamePair(typeof(T), Name), stateType.FullName));
            }

            CurrentStateBase.OnLeave(this, false);
            m_CurrentStateTime = 0f;
            CurrentStateBase = state;
            CurrentStateBase.OnEnter(this);
        }
    }
}