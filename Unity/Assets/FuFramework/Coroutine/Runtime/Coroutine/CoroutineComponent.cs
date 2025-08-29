using System.Collections;
using System.Collections.Concurrent;
using FuFramework.Core.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Coroutine.Runtime
{
    /// <summary>
    /// 协程组件
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Coroutine")]
    public class CoroutineComponent : FuComponent
    {
        /// <summary>
        /// 等待帧结束
        /// </summary>
        private readonly WaitForEndOfFrame _waitForEndOfFrame = new();

        /// <summary>
        /// 执行过的迭代器
        /// </summary>
        private readonly ConcurrentDictionary<IEnumerator, UnityEngine.Coroutine> m_CoroutineMap = new();

        protected override void OnInit() { }
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds) { }
        protected override void OnShutdown(ShutdownType shutdownType) { }

        /// <summary>
        /// 开启一个协程
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public new void StartCoroutine(IEnumerator enumerator)
        {
            var ret = base.StartCoroutine(enumerator);
            m_CoroutineMap[enumerator] = ret;
        }

        /// <summary>
        /// 终止一个协程
        /// </summary>
        /// <param name="enumerator"></param>
        public new void StopCoroutine(IEnumerator enumerator)
        {
            if (m_CoroutineMap.TryGetValue(enumerator, out var coroutine))
            {
                base.StopCoroutine(coroutine);
                m_CoroutineMap.TryRemove(enumerator, out _);
            }

            base.StopCoroutine(enumerator);
        }

        /// <summary>
        /// 终止一个协程
        /// </summary>
        /// <param name="coroutine"></param>
        public new void StopCoroutine(UnityEngine.Coroutine coroutine)
        {
            base.StopCoroutine(coroutine);
            foreach (var item in m_CoroutineMap)
            {
                if (item.Value == coroutine)
                {
                    m_CoroutineMap.TryRemove(item.Key, out _);
                    break;
                }
            }
        }

        /// <summary>
        /// 终止全部协程
        /// </summary>
        public new void StopAllCoroutines()
        {
            foreach (var coroutine in m_CoroutineMap.Values)
            {
                base.StopCoroutine(coroutine);
            }

            m_CoroutineMap.Clear();
            base.StopAllCoroutines();
        }

        /// <summary>
        /// 等待当前帧结束
        /// </summary>
        /// <returns></returns>
        private IEnumerator _WaitForEndOfFrameFinish(System.Action callback)
        {
            yield return _waitForEndOfFrame;
            callback?.Invoke();
        }

        /// <summary>
        /// 等待当前帧结束
        /// </summary>
        /// <param name="callback"></param>
        public void WaitForEndOfFrameFinish(System.Action callback)
        {
            StartCoroutine(_WaitForEndOfFrameFinish(callback));
        }
    }
}