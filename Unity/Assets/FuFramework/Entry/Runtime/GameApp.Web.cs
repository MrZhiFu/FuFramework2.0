using FuFramework.Core.Runtime;
using FuFramework.Web.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static WebComponent _web;

        /// <summary>
        /// 获取Web组件。
        /// </summary>
        public static WebComponent Web
        {
            get
            {
                if (!_web) _web = GameEntry.GetComponent<WebComponent>();
                return _web;
            }
        }
    }
}