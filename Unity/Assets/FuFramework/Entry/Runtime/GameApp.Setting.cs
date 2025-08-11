using FuFramework.Core.Runtime;
using GameFrameX.Setting.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取配置组件。
    /// </summary>
    public static SettingComponent Setting
    {
        get
        {
            if (_setting == null)
            {
                _setting = GameEntry.GetComponent<SettingComponent>();
            }

            return _setting;
        }
    }

    private static SettingComponent _setting;
}