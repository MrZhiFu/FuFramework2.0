using UnityEngine;

namespace LuBan.Runtime
{
    /// <summary>
    /// 防止代码运行时发生裁剪报错。将这个脚本添加到启动场景中。不会对逻辑有任何影响
    /// </summary>
    public class LuBanCroppingHelper : MonoBehaviour
    {
        void Start()
        {
            _ = typeof(BeanBase);
            _ = typeof(EDeserializeError);
            _ = typeof(SerializationException);
            _ = typeof(SegmentSaveState);
            _ = typeof(ByteBuf);
            _ = typeof(ITypeId);
            _ = typeof(StringUtil);
        }
    }
}