using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    public sealed partial class TimerManager
    {
        /// <summary>
        /// 定时器项
        /// </summary>
        private class TimerItem
        {
            /// <summary>
            /// 间隔时间（以秒为单位）
            /// </summary>
            public float Interval;

            /// <summary>
            /// 重复次数（0 表示无限重复）
            /// </summary>
            public int Repeat;

            /// <summary>
            /// 已过时间（以秒为单位）
            /// </summary>
            public float Elapsed;

            /// <summary>
            /// 是否已删除
            /// </summary>
            public bool Deleted;

            /// <summary>
            /// 回调函数参数
            /// </summary>
            public object Param;

            /// <summary>
            /// 回调函数
            /// </summary>
            public Action<object> Callback;

            /// <summary>
            /// 设置一个定时器
            /// </summary>
            /// <param name="interval"> 间隔时间 </param>
            /// <param name="repeat"> 重复次数 </param>
            /// <param name="callback"> 回调函数 </param>
            /// <param name="param"> 回调函数参数 </param>
            public void Set(float interval, int repeat, Action<object> callback, object param)
            {
                Interval = interval;
                Repeat   = repeat;
                Callback = callback;
                Param    = param;
            }
        }
    }
}