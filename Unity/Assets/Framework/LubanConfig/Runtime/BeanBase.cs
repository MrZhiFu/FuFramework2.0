using UnityEngine.Scripting;

namespace LuBan.Runtime
{
    [Preserve]
    public abstract class BeanBase : ITypeId
    {
        public abstract int GetTypeId();
    }
}