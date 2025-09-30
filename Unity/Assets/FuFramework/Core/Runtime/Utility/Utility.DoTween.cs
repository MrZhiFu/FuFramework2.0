using System;
using DG.Tweening;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// Do Tween 工具类，封装了一些常用的数值动画效果。
        /// 1. 关闭对象上的全部动画。
        /// 2. 从浮点值到另一个值。
        /// 3. 从整形值到另一个值。
        /// 4. 从无符号整形值到另一个值。
        /// 5. 从长整形值到另一个值。
        /// 6. 从无符号长整形值到另一个值。
        /// 7. 从Vector3到另一个值。
        /// </summary>
        public static class DoTween
        {
            /// <summary>
            /// 关闭对象上的全部动画
            /// </summary>
            /// <param name="gameObject">物体对象</param>
            /// <param name="complete">是否直接完成动画</param>
            public static void Kill(GameObject gameObject, bool complete = false) => DOTween.Kill(gameObject, complete);

            /// <summary>
            /// 从浮点值到另一个浮点值，带有完成回调
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(float startValue, float endValue, float time, Action<float> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time).OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从整形值到另一个整形值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(int startValue, int endValue, float time, Action<int> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time).OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从无符号整形值到另一个无符号整形值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(uint startValue, uint endValue, float time, Action<uint> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time).OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从长整形值到另一个长整形值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(long startValue, long endValue, float time, Action<long> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time).OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从无符号长整形值到另一个无符号长整形值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(ulong startValue, ulong endValue, float time, Action<ulong> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time).OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从Vector3值到另一个Vector3值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(Vector3 startValue, Vector3 endValue, float time, Action<Vector3> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time)
                    .OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从Vector3Int值到另一个Vector3Int值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(Vector3Int startValue, Vector3Int endValue, float time, Action<Vector3Int> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(new Vector3Int((int)m.x, (int)m.y, (int)m.z)); }, endValue, time)
                    .OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从Vector2值到另一个Vector2值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(Vector2 startValue, Vector2 endValue, float time, Action<Vector2> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(m); }, endValue, time)
                    .OnComplete(() => { complete?.Invoke(); });
            }

            /// <summary>
            /// 从Vector2Int值到另一个Vector2Int值
            /// </summary>
            /// <param name="startValue">开始值</param>
            /// <param name="endValue">结束值</param>
            /// <param name="time">持续时长</param>
            /// <param name="update">更新回调</param>
            /// <param name="complete">完成回调</param>
            /// <returns></returns>
            public static Tweener To(Vector2Int startValue, Vector2Int endValue, float time, Action<Vector2Int> update, Action complete = null)
            {
                return DOTween.To(() => startValue, m => { update?.Invoke(new Vector2Int((int)m.x, (int)m.y)); }, endValue, time)
                    .OnComplete(() => { complete?.Invoke(); });
            }
        }
    }
}