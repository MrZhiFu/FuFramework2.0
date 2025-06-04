using UnityEngine;
using UnityEngine.Scripting;

namespace LuBan.Runtime
{
    /// <summary>
    /// 防止代码运行时发生裁剪报错。将这个脚本添加到启动场景中。不会对逻辑有任何影响
    /// </summary>
    [Preserve]
    public class LuBanCroppingHelper : MonoBehaviour
    {
        void Start()
        {
            _ = typeof(LuBan.Runtime.BeanBase);
            _ = typeof(LuBan.Runtime.EDeserializeError);
            _ = typeof(LuBan.Runtime.SerializationException);
            _ = typeof(LuBan.Runtime.SegmentSaveState);
            _ = typeof(LuBan.Runtime.ByteBuf);
            _ = typeof(LuBan.Runtime.ITypeId);
            _ = typeof(LuBan.Runtime.StringUtil);
        }
    }
}