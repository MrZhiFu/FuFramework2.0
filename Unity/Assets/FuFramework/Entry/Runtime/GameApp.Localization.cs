using FuFramework.Core.Runtime;
using FuFramework.Localization.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static LocalizationComponent _localization;

        /// <summary>
        /// 获取本地化组件。
        /// </summary>
        public static LocalizationComponent Localization
        {
            get
            {
                if (!_localization) _localization = GameEntry.GetComponent<LocalizationComponent>();
                return _localization;
            }
        }
    }
}