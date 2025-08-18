using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static ProcedureComponent _procedure;

        /// <summary>
        /// 获取流程组件。
        /// </summary>
        public static ProcedureComponent Procedure
        {
            get
            {
                if (!_procedure) _procedure = GameEntry.GetComponent<ProcedureComponent>();
                return _procedure;
            }
        }
    }
}