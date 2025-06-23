using UnityEngine.Scripting;

namespace LuBan.Runtime
{
   
    public abstract class BeanBase : ITypeId
    {
        public abstract int GetTypeId();
    }
}