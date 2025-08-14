using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static BaseComponent _base;

        /// <summary>
        /// 获取游戏基础组件。
        /// </summary>
        public static BaseComponent Base
        {
            get
            {
                if (!_base) _base = GameEntry.GetComponent<BaseComponent>();
                return _base;
            }
        }
    }
}