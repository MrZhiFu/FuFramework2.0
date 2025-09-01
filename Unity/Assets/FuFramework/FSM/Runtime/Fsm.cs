using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Runtime
{
    /// <summary>
    /// 有限状态机。
    /// </summary>
    public sealed class Fsm : IReference
    {
        /// <summary>
        /// 记录该有限状态机的所有状态的字典。key为状态类型，value为状态实例。
        /// </summary>
        private readonly Dictionary<Type, FsmStateBase> m_StateDict = new();

        /// <summary>
        /// 记录该有限状态机的所有数据变量的字典。key为变量名，value为变量实例。
        /// </summary>
        private Dictionary<string, Variable.Runtime.Variable> m_DataDict;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 获取有限状态机完整名称。
        /// </summary>
        public string FullName => new TypeNamePair(OwnerType, Name).ToString();

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
        public Type Owner { get; private set; }

        /// <summary>
        /// 获取当前有限状态机状态。
        /// </summary>
        public FsmStateBase CurrentStateBase { get; private set; }


        /// <summary>
        /// 获取有限状态机持有者类型。
        /// </summary>
        public Type OwnerType => Owner;

        /// <summary>
        /// 获取有限状态机中状态的数量。
        /// </summary>
        public int FsmStateCount => m_StateDict.Count;

        /// <summary>
        /// 获取有限状态机是否正在运行。
        /// </summary>
        public bool IsRunning => CurrentStateBase != null;

        /// <summary>
        /// 获取有限状态机是否被销毁。
        /// </summary>
        public bool IsDestroyed => m_IsDestroyed;

        /// <summary>
        /// 获取当前有限状态机状态名称。
        /// </summary>
        public string CurrentStateName => CurrentStateBase?.GetType().FullName;

        /// <summary>
        /// 获取当前有限状态机状态持续时间。
        /// </summary>
        public float CurrentStateTime => m_CurrentStateTime;


        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm Create<T>(string name, T owner, params FsmStateBase[] states) where T : class
        {
            if (owner == null) throw new FuException("FSM owner is invalid.");
            if (states == null || states.Length < 1) throw new FuException("FSM states is invalid.");

            var fsm = ReferencePool.Runtime.ReferencePool.Acquire<Fsm>();
            fsm.Name          = name;
            fsm.Owner         = owner.GetType();
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
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm Create<T>(string name, T owner, List<FsmStateBase> states) where T : class
        {
            if (owner == null) throw new FuException("FSM owner is invalid.");
            if (states == null || states.Count < 1) throw new FuException("FSM states is invalid.");

            var fsm = ReferencePool.Runtime.ReferencePool.Acquire<Fsm>();
            fsm.Name          = name;
            fsm.Owner         = owner.GetType();
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
        public void Start<TState>() where TState : FsmStateBase
        {
            if (IsRunning) throw new FuException("FSM is running, can not start again.");

            FsmStateBase stateBase = GetState<TState>();

            m_CurrentStateTime = 0f;
            CurrentStateBase   = stateBase ?? throw new FuException(Utility.Text.Format("FSM '{0}' can not start state '{1}' which is not exist.", FullName, typeof(TState).FullName));
            CurrentStateBase.OnEnter();
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <param name="stateType">要开始的有限状态机状态类型。</param>
        public void Start(Type stateType)
        {
            if (IsRunning) throw new FuException("FSM is running, can not start again.");
            if (stateType == null) throw new FuException("State type is invalid.");

            if (!typeof(FsmStateBase).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));

            var state = GetState(stateType);

            m_CurrentStateTime = 0f;
            CurrentStateBase   = state ?? throw new FuException(Utility.Text.Format("FSM '{0}' can not start state '{1}' which is not exist.", FullName, stateType.FullName));
            CurrentStateBase.OnEnter();
        }

        /// <summary>
        /// 有限状态机轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (CurrentStateBase == null) return;
            m_CurrentStateTime += elapseSeconds;
            CurrentStateBase.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 清理有限状态机。
        /// </summary>
        public void Clear()
        {
            CurrentStateBase?.OnLeave(true);

            foreach (var (_, state) in m_StateDict)
            {
                state.OnDestroy();
            }

            Name  = null;
            Owner = null;
            m_StateDict.Clear();

            if (m_DataDict != null)
            {
                foreach (var (_, data) in m_DataDict)
                {
                    if (data == null) continue;
                    ReferencePool.Runtime.ReferencePool.Release(data);
                }

                m_DataDict.Clear();
            }

            CurrentStateBase   = null;
            m_CurrentStateTime = 0f;
            m_IsDestroyed      = true;
        }

        /// <summary>
        /// 关闭并清理有限状态机。
        /// </summary>
        internal void Shutdown() => ReferencePool.Runtime.ReferencePool.Release(this);

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要检查的有限状态机状态类型。</typeparam>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState<TState>() where TState : FsmStateBase
        {
            return m_StateDict.ContainsKey(typeof(TState));
        }

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <param name="stateType">要检查的有限状态机状态类型。</param>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState(Type stateType)
        {
            if (stateType == null) throw new FuException("State type is invalid.");
            if (!typeof(FsmStateBase).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            return m_StateDict.ContainsKey(stateType);
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要获取的有限状态机状态类型。</typeparam>
        /// <returns>要获取的有限状态机状态。</returns>
        public TState GetState<TState>() where TState : FsmStateBase
        {
            return m_StateDict.TryGetValue(typeof(TState), out var state) ? state as TState : null;
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <param name="stateType">要获取的有限状态机状态类型。</param>
        /// <returns>要获取的有限状态机状态。</returns>
        public FsmStateBase GetState(Type stateType)
        {
            if (stateType == null) throw new FuException("State type is invalid.");
            if (!typeof(FsmStateBase).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            return m_StateDict.GetValueOrDefault(stateType);
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态。</returns>
        public FsmStateBase[] GetAllStates()
        {
            var index   = 0;
            var results = new FsmStateBase[m_StateDict.Count];
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
        public void GetAllStates(List<FsmStateBase> results)
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
        public TData GetData<TData>(string name) where TData : Variable.Runtime.Variable
        {
            return GetData(name) as TData;
        }

        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        public Variable.Runtime.Variable GetData(string name)
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
        public void SetData<TData>(string name, TData data) where TData : Variable.Runtime.Variable
        {
            SetData(name, data as Variable.Runtime.Variable);
        }

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <param name="data">要设置的有限状态机数据。</param>
        public void SetData(string name, Variable.Runtime.Variable data)
        {
            if (string.IsNullOrEmpty(name)) throw new FuException("Data name is invalid.");

            m_DataDict ??= new Dictionary<string, Variable.Runtime.Variable>(StringComparer.Ordinal);

            var oldData = GetData(name);
            if (oldData != null)
                ReferencePool.Runtime.ReferencePool.Release(oldData);

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
            if (oldData != null) ReferencePool.Runtime.ReferencePool.Release(oldData);
            return m_DataDict.Remove(name);
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
        internal void ChangeState<TState>() where TState : FsmStateBase
        {
            ChangeState(typeof(TState));
        }

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
                throw new FuException(Utility.Text.Format("FSM '{0}' can not change state to '{1}' which is not exist.", FullName, stateType.FullName));
            }

            CurrentStateBase.OnLeave(false);
            m_CurrentStateTime = 0f;
            CurrentStateBase   = state;
            CurrentStateBase.OnEnter();
        }
    }
}