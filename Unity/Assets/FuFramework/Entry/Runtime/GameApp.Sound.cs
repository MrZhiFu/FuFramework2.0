using FuFramework.Core.Runtime;
using GameFrameX.Sound.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static SoundComponent _sound;

        /// <summary>
        /// 获取声音组件。
        /// </summary>
        public static SoundComponent Sound
        {
            get
            {
                if (!_sound) _sound = GameEntry.GetComponent<SoundComponent>();
                return _sound;
            }
        }
    }
}