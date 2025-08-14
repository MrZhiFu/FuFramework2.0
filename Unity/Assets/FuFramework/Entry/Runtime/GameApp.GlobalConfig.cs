using FuFramework.Core.Runtime;
using GameFrameX.GlobalConfig.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static GlobalConfigComponent _globalConfig;

        /// <summary>
        /// 获取全局配置组件。
        /// </summary>
        public static GlobalConfigComponent GlobalConfig
        {
            get
            {
                if (!_globalConfig) _globalConfig = GameEntry.GetComponent<GlobalConfigComponent>();
                return _globalConfig;
            }
        }
    }
}