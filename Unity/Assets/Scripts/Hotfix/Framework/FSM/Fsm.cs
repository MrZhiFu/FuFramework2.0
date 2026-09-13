using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.FSM
{
    /// <summary>
    /// 有限状态机。
    /// 功能：
    ///     1. 用于管理一个有限状态机下的状态和数据变量。
    ///     2. 创建一个有限状态机实例。
    ///     3. 提供一些便捷的方法，如切换状态、设置数据变量等。
    /// </summary>
    public sealed class Fsm : IReference
    {
        /// <summary>
        /// 记录该有限状态机的所有状态的字典。key为状态类型，value为状态实例。
        /// </summary>
        private readonly Dictionary<Type, FsmStateBase> m_StateDict = new();

        /// <summary>
        /// 有限状态机共享数据表（通用存储：值为任意对象，不绑定具体变量类型）。
        /// 约定：若存入的对象实现 <see cref="IReference"/>（引用池对象），由 Fsm 负责在替换/移除/销毁时回收；
        /// 普通对象则仅丢弃引用，由 GC 回收。
        /// </summary>
        private Dictionary<string, object> m_DataDict;

        /// <summary>
        /// <see cref="Clear"/> 期间用于「同一池对象被挂在多个数据键下」去重的复用集合。
        /// Fsm 自身即池对象，销毁路径不应产生托管分配，故复用实例字段而非每次 new HashSet。
        /// </summary>
        private readonly HashSet<IReference> m_RecycledData = new();

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 获取有限状态机持有者。
        /// </summary>
        public Type Owner { get; private set; }

        /// <summary>
        /// 获取有限状态机是否被销毁。
        /// </summary>
        public bool IsDestroyed { get; private set; } = true;

        /// <summary>
        /// 获取当前有限状态机状态持续时间。
        /// </summary>
        public float CurrentStateTime { get; private set; }

        /// <summary>
        /// 获取当前有限状态机状态。
        /// </summary>
        public FsmStateBase CurrentStateBase { get; private set; }


        /// <summary>
        /// 获取有限状态机完整名称。
        /// </summary>
        public string FullName => new TypeNamePair(Owner, Name).ToString();

        /// <summary>
        /// 获取有限状态机中状态的数量。
        /// </summary>
        public int FsmStateCount => m_StateDict.Count;

        /// <summary>
        /// 获取有限状态机是否正在运行。
        /// </summary>
        public bool IsRunning => CurrentStateBase != null;

        /// <summary>
        /// 获取当前有限状态机状态名称。
        /// </summary>
        public string CurrentStateName => CurrentStateBase?.GetType().FullName;


        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm Create<T>(string name, T owner, params FsmStateBase[] states) where T : class
        {
            if (owner == null) throw new InvalidOperationException("[Fsm] 有限状态机持有者不能为空.");
            if (states == null || states.Length < 1) throw new InvalidOperationException("[Fsm] 有限状态机状态不能为空.");

            // 校验全部前置到 Acquire 之前：否则 Acquire 之后再抛异常会漏掉回收，使该 Fsm 永久占用引用池的「使用中」计数
            ValidateStates(states, typeof(T), name);

            var fsm = ReferencePool.Acquire<Fsm>();
            fsm.Name        = name;
            fsm.Owner       = owner.GetType();
            fsm.IsDestroyed = false;

            try
            {
                for (var i = 0; i < states.Length; i++)
                {
                    var state     = states[i];
                    var stateType = state.GetType();
                    // 类型唯一性已前置校验，此处不会因重复而失败
                    fsm.m_StateDict.Add(stateType, state);

                    // 初始化状态(用户代码，可能抛异常)
                    state.OnInit(fsm);
                }
            }
            catch
            {
                // OnInit 抛异常时，已 Add 的状态仍留在 fsm.m_StateDict 中；Recycle → Clear 会统一 OnDestroy、清空字典并复位，
                // 既避免半成品 Fsm 漏回收，也不会让已入表的状态残留。
                ReferencePool.Recycle(fsm);
                throw;
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
            if (owner == null) throw new InvalidOperationException("[Fsm] 有限状态机持有者不能为空.");
            if (states == null || states.Count < 1) throw new InvalidOperationException("[Fsm] 有限状态机状态不能为空.");

            // 校验全部前置到 Acquire 之前：否则 Acquire 之后再抛异常会漏掉回收，使该 Fsm 永久占用引用池的「使用中」计数
            ValidateStates(states, typeof(T), name);

            var fsm = ReferencePool.Acquire<Fsm>();
            fsm.Name        = name;
            fsm.Owner       = owner.GetType();
            fsm.IsDestroyed = false;

            try
            {
                for (var i = 0; i < states.Count; i++)
                {
                    var state     = states[i];
                    var stateType = state.GetType();
                    // 类型唯一性已前置校验，此处不会因重复而失败
                    fsm.m_StateDict.Add(stateType, state);

                    // 初始化状态(用户代码，可能抛异常)
                    state.OnInit(fsm);
                }
            }
            catch
            {
                // 同 params 重载：已 Add 的状态交由 Recycle → Clear 统一 OnDestroy、清空字典并复位
                ReferencePool.Recycle(fsm);
                throw;
            }

            return fsm;
        }

        /// <summary>
        /// 校验待创建有限状态机的状态集合：元素非空且状态类型不重复。
        /// 由两个 Create 重载在 Acquire 之前调用，保证校验失败时不会有已获取的 Fsm 漏回收。
        /// </summary>
        /// <param name="states">待校验的状态集合。</param>
        /// <param name="ownerType">有限状态机持有者类型（仅用于异常消息）。</param>
        /// <param name="name">有限状态机名称（仅用于异常消息）。</param>
        private static void ValidateStates(IList<FsmStateBase> states, Type ownerType, string name)
        {
            for (var i = 0; i < states.Count; i++)
            {
                if (states[i] == null) throw new InvalidOperationException("[Fsm] 有限状态机状态不能为空.");
            }

            // 重复状态类型检测：状态数量通常为个位数，用 O(n²) 手写比较即可，无需为此一次性校验引入 LINQ 或分配 HashSet
            for (var i = 0; i < states.Count; i++)
            {
                var stateType = states[i].GetType();
                for (var j = 0; j < i; j++)
                {
                    if (states[j].GetType() != stateType) continue;
                    throw new InvalidOperationException($"[Fsm] 有限状态机 '{new TypeNamePair(ownerType, name)}' 状态 '{stateType.FullName}' 已经存在，不能重复添加.");
                }
            }
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <typeparam name="TState">要开始的有限状态机状态类型。</typeparam>
        public void Start<TState>() where TState : FsmStateBase
        {
            if (IsRunning) throw new InvalidOperationException("[Fsm] 有限状态机正在运行中，不能重复开始。");

            FsmStateBase stateBase = GetState<TState>();

            CurrentStateTime = 0f;
            CurrentStateBase = stateBase ?? throw new InvalidOperationException($"[Fsm] 有限状态机 '{FullName}' 开始状态 '{typeof(TState).FullName}' 失败，状态不存在。");
            CurrentStateBase.OnEnter();
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <param name="stateType">要开始的有限状态机状态类型。</param>
        public void Start(Type stateType)
        {
            if (IsRunning) throw new InvalidOperationException("[Fsm] 有限状态机正在运行中，不能重复开始。");
            if (stateType == null) throw new InvalidOperationException("[Fsm] 有限状态机开始失败，需要开始的状态不能为空。");

            if (!typeof(FsmStateBase).IsAssignableFrom(stateType))
                throw new InvalidOperationException($"State type '{stateType.FullName}' is invalid.");

            var state = GetState(stateType);

            CurrentStateTime = 0f;
            CurrentStateBase = state ?? throw new InvalidOperationException($"[Fsm] 有限状态机 '{FullName}' 开始状态 '{stateType.FullName}' 失败，状态不存在。");
            CurrentStateBase.OnEnter();
        }

        /// <summary>
        /// 有限状态机轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        internal void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (CurrentStateBase == null) return;
            CurrentStateTime += deltaTime;
            CurrentStateBase.OnUpdate(deltaTime, unscaledDeltaTime);
        }

        /// <summary>
        /// 清理有限状态机。
        /// </summary>
        public void Clear()
        {
            // 逐项异常隔离：OnLeave/OnDestroy/Recycle 都是用户代码（OnDestroy 可能回收池对象、抛异常），
            // 任一抛出都不应中断后续状态与数据对象的回收、也不应让本 Fsm 回不了池——
            // 回收语义单调：进入 Clear 就必须把 Fsm 完整复位并归还引用池。
            try
            {
                CurrentStateBase?.OnLeave(true);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[Fsm] 状态 {CurrentStateBase?.GetType().Name} OnLeave 异常: {e.Message}");
            }

            foreach (var (_, state) in m_StateDict)
            {
                try
                {
                    state.OnDestroy();
                }
                catch (Exception e)
                {
                    FuLogger.LogError($"[Fsm] 状态 {state.GetType().Name} OnDestroy 异常: {e.Message}");
                }
            }

            Name  = null;
            Owner = null;
            m_StateDict.Clear();

            if (m_DataDict != null)
            {
                // 同一池对象可能被挂在多个键下，先去重再回收：
                // 否则二次 Recycle 会抛异常并沿 Shutdown→ReferencePool.Recycle(this) 传播，导致整个 Fsm 回不了池。
                // 复用实例字段（Fsm 是池对象，销毁路径不应分配 HashSet）；用后即清，避免闲置在池中的 Fsm 长期持有已回收对象。
                m_RecycledData.Clear();
                foreach (var (_, data) in m_DataDict)
                {
                    try
                    {
                        // 仅回收池化对象；普通对象丢弃引用即可
                        if (data is IReference reference && m_RecycledData.Add(reference))
                            ReferencePool.Recycle(reference);
                    }
                    catch (Exception e)
                    {
                        FuLogger.LogError($"[Fsm] 数据对象回收异常: {e.Message}");
                    }
                }
                m_RecycledData.Clear();

                m_DataDict.Clear();
            }

            CurrentStateBase = null;
            CurrentStateTime = 0f;
            IsDestroyed      = true;
        }

        /// <summary>
        /// 关闭并清理有限状态机。
        /// </summary>
        internal void Shutdown() => ReferencePool.Recycle(this);

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
            if (stateType == null) throw new InvalidOperationException("[Fsm] 需要判断的状态不能为空。");
            if (!typeof(FsmStateBase).IsAssignableFrom(stateType))
                throw new InvalidOperationException($"[Fsm] 状态类型 '{stateType.FullName}' 不是 FsmStateBase 的子类。");
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
            if (stateType == null) throw new InvalidOperationException("[Fsm] 需要获取的状态不能为空。");
            if (!typeof(FsmStateBase).IsAssignableFrom(stateType))
                throw new InvalidOperationException($"[Fsm] 状态类型 '{stateType.FullName}' 不是 FsmStateBase 的子类。");
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
            if (results == null) throw new InvalidOperationException("[Fsm] 结果列表不能为空。");
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
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[Fsm] 数据名称不能为空。");
            return m_DataDict != null && m_DataDict.ContainsKey(name);
        }

        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <typeparam name="TData">要获取的有限状态机数据的类型。</typeparam>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        public TData GetData<TData>(string name)
        {
            return GetData(name) is TData data ? data : default;
        }

        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        public object GetData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[Fsm] 数据名称不能为空。");
            return m_DataDict?.GetValueOrDefault(name);
        }

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <typeparam name="TData">要设置的有限状态机数据的类型。</typeparam>
        /// <param name="name">有限状态机数据名称。</param>
        /// <param name="data">要设置的有限状态机数据。</param>
        public void SetData<TData>(string name, TData data)
        {
            SetData(name, (object)data);
        }

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <param name="data">要设置的有限状态机数据。</param>
        public void SetData(string name, object data)
        {
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[Fsm] 需要设置的数据名称不能为空。");

            m_DataDict ??= new Dictionary<string, object>(StringComparer.Ordinal);

            // 覆盖旧值时，仅回收池化对象；普通对象丢弃引用即可。
            // 必须排除「新旧为同一实例」：否则会把仍被本键持有的对象归还池，造成跨系统污染（同一实例被两个持有者复用）。
            // 还须排除「旧值仍被其它键引用」：SetData("A",x); SetData("B",x); SetData("A",y) 时 x 仍被键 B 持有，
            // 直接 Recycle 会造成 use-after-recycle —— 键 B 后续按 x 使用(数据已被 Clear 复位)，其回收时二次 Recycle 抛异常，
            // 并沿 Shutdown→ReferencePool.Recycle(this) 传播致 Fsm 回不了池。判定口径与 RemoveData 完全一致。
            if (m_DataDict.TryGetValue(name, out var oldData) && !ReferenceEquals(oldData, data) &&
                oldData is IReference reference && !IsReferencedByOtherDataKeys(name, reference))
                ReferencePool.Recycle(reference);

            m_DataDict[name] = data;
        }

        /// <summary>
        /// 移除有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>是否移除有限状态机数据成功。</returns>
        public bool RemoveData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[Fsm] 需要移除的数据名称不能为空。");
            if (m_DataDict == null) return false;

            // 仅回收池化对象；普通对象丢弃引用即可。
            // 回收前必须先确认没有其它键仍引用同一实例：SetData("A",x); SetData("B",x); RemoveData("A") 时 x 仍被键 B 持有，
            // 直接 Recycle 会造成 use-after-recycle —— 键 B 后续按 x 使用(数据已被 Clear 复位)，其回收时二次 Recycle 抛异常，
            // 并沿 Shutdown→ReferencePool.Recycle(this) 传播致 Fsm 回不了池。
            if (m_DataDict.TryGetValue(name, out var oldData) && oldData is IReference reference && !IsReferencedByOtherDataKeys(name, reference))
                ReferencePool.Recycle(reference);

            return m_DataDict.Remove(name);
        }

        /// <summary>
        /// 判断指定池对象是否仍被除 <paramref name="excludedName"/> 之外的其它数据键引用（手写循环，避免 LINQ 分配）。
        /// </summary>
        /// <param name="excludedName">被排除的数据键（即当前正在移除的键）。</param>
        /// <param name="reference">待判定的池对象。</param>
        /// <returns>是否仍有其它键引用该对象。</returns>
        private bool IsReferencedByOtherDataKeys(string excludedName, IReference reference)
        {
            // 键比较走 StringComparison.Ordinal，与 m_DataDict 的 StringComparer.Ordinal 口径一致
            foreach (var (key, data) in m_DataDict)
            {
                if (string.Equals(key, excludedName, StringComparison.Ordinal)) continue;
                if (ReferenceEquals(data, reference)) return true;
            }

            return false;
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
            if (CurrentStateBase == null) throw new InvalidOperationException("[Fsm] 有限状态机当前状态为空，切换失败。");
            var state = GetState(stateType);
            if (state == null) throw new InvalidOperationException($"[Fsm] 有限状态机 '{FullName}' 切换状态 '{stateType.FullName}' 失败，状态不存在。");

            CurrentStateBase.OnLeave(false);
            CurrentStateTime = 0f;
            CurrentStateBase = state;
            CurrentStateBase.OnEnter();
        }
    }
}
