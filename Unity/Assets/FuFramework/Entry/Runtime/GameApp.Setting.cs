using FuFramework.Core.Runtime;
using GameFrameX.Setting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static SettingComponent _setting;

        /// <summary>
        /// 获取配置组件。
        /// </summary>
        public static SettingComponent Setting
        {
            get
            {
                if (!_setting) _setting = GameEntry.GetComponent<SettingComponent>();
                return _setting;
            }
        }
    }
}