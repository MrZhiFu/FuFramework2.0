namespace GameFrameX.Runtime
{
    /// <summary>
    /// 对象帮助类
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public static class ObjectHelper
    {
        /// <summary>
        /// 交换两个引用
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <typeparam name="T"></typeparam>
        [UnityEngine.Scripting.Preserve]
        public static void Swap<T>(ref T t1, ref T t2)
        {
            (t1, t2) = (t2, t1);
        }
    }
}