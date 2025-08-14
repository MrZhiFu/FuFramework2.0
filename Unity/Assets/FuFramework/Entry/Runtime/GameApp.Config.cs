using FuFramework.Config.Runtime;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static ConfigComponent _config;

        /// <summary>
        /// 获取配置组件。
        /// </summary>
        public static ConfigComponent Config
        {
            get
            {
                if (!_config) _config = GameEntry.GetComponent<ConfigComponent>();
                return _config;
            }
        }
    }
}