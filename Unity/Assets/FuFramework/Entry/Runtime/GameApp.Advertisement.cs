#if ENABLE_GAME_FRAME_X_ADVERTISEMENT
using GameFrameX.Runtime;
using GameFrameX.Advertisement.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取广告组件。
    /// </summary>
    public static AdvertisementComponent Advertisement
    {
        get
        {
            if (_advertisement == null)
            {
                _advertisement = GameEntry.GetComponent<AdvertisementComponent>();
            }

            return _advertisement;
        }
    }

    private static AdvertisementComponent _advertisement;

}
#endif