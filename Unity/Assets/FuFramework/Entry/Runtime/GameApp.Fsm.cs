using FuFramework.Core.Runtime;
using FuFramework.Fsm.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static FsmComponent _fsm;

        /// <summary>
        /// 获取有限状态机组件。
        /// </summary>
        public static FsmComponent Fsm
        {
            get
            {
                if (!_fsm) _fsm = GameEntry.GetComponent<FsmComponent>();
                return _fsm;
            }
        }
    }
}