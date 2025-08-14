#if ENABLE_GAME_FRAME_X_ADVERTISEMENT
using GameFrameX.Runtime;
using GameFrameX.Advertisement.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static AdvertisementComponent _advertisement;
        
/// <summary>
        /// 获取广告组件。
        /// </summary>
        public static AdvertisementComponent Advertisement
        {
            get
            {
                if (!_advertisement) _advertisement = GameEntry.GetComponent<AdvertisementComponent>();
                return _advertisement;
            }
        }
    }
}
#endif