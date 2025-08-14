using FuFramework.Core.Runtime;
using GameFrameX.Mono.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static MonoComponent _mono;

        /// <summary>
        /// 获取Mono组件。
        /// </summary>
        public static MonoComponent Mono
        {
            get
            {
                if (!_mono) _mono = GameEntry.GetComponent<MonoComponent>();
                return _mono;
            }
        }
    }
}