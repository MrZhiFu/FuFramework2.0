using FuFramework.Core.Runtime;
using FuFramework.Coroutine.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static CoroutineComponent _coroutine;

        /// <summary>
        /// 获取协程组件。
        /// </summary>
        public static CoroutineComponent Coroutine
        {
            get
            {
                if (!_coroutine) _coroutine = GameEntry.GetComponent<CoroutineComponent>();
                return _coroutine;
            }
        }
    }
}