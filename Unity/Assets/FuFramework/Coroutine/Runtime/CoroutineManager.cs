using System.Collections;
using System.Collections.Concurrent;
using FuFramework.Core.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Coroutine.Runtime
{
    /// <summary>
    /// 协程管理器。
    /// 用于管理协程，提供一些便捷的方法。推荐还是使用UniTask实现。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Coroutine")]
    public class CoroutineManager : FuComponent
    {
        /// <summary>
        /// 等待帧结束
        /// </summary>
        private readonly WaitForEndOfFrame m_waitForEndOfFrame = new();

        /// <summary>
        /// 记录执行的协程迭字典，key为迭代器对象，value为Unity的协程对象
        /// </summary>
        private readonly ConcurrentDictionary<IEnumerator, UnityEngine.Coroutine> m_CoroutineDict = new();

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType) => StopAllCoroutines();

        /// <summary>
        /// 开启一个协程
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public new void StartCoroutine(IEnumerator enumerator)
        {
            var ret = base.StartCoroutine(enumerator);
            m_CoroutineDict[enumerator] = ret;
        }

        /// <summary>
        /// 终止一个协程
        /// </summary>
        /// <param name="enumerator"></param>
        public new void StopCoroutine(IEnumerator enumerator)
        {
            if (m_CoroutineDict.TryGetValue(enumerator, out var coroutine))
            {
                base.StopCoroutine(coroutine);
                m_CoroutineDict.TryRemove(enumerator, out _);
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
            foreach (var item in m_CoroutineDict)
            {
                if (item.Value == coroutine)
                {
                    m_CoroutineDict.TryRemove(item.Key, out _);
                    break;
                }
            }
        }

        /// <summary>
        /// 终止全部协程
        /// </summary>
        public new void StopAllCoroutines()
        {
            foreach (var coroutine in m_CoroutineDict.Values)
            {
                base.StopCoroutine(coroutine);
            }

            m_CoroutineDict.Clear();
            base.StopAllCoroutines();
        }

        /// <summary>
        /// 等待当前帧结束
        /// </summary>
        /// <param name="callback"></param>
        public void WaitForEndOfFrameFinish(System.Action callback)
        {
            StartCoroutine(_WaitForEndOfFrameFinish(callback));
        }

        /// <summary>
        /// 等待当前帧结束
        /// </summary>
        /// <returns></returns>
        private IEnumerator _WaitForEndOfFrameFinish(System.Action callback)
        {
            yield return m_waitForEndOfFrame;
            callback?.Invoke();
        }
    }
}