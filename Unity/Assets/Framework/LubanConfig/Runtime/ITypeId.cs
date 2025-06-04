using UnityEngine.Scripting;

namespace LuBan.Runtime
{
    [Preserve]
    public interface ITypeId
    {
        int GetTypeId();
    }
}