using Newtonsoft.Json;
using Utility = Hotfix.Framework.Core.Utility;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using ProtoBuf;

// ReSharper disable once CheckNamespace
using AOT.Framework.Core.Utility;
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// HTTP消息包装基类
    /// </summary>
    [ProtoContract]
    public class MessageHttpObject
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [ProtoMember(1)]
        public int Id { get; set; }

        /// <summary>
        /// 消息序列号
        /// </summary>
        [ProtoMember(2)]
        public int UniqueId { get; set; }

        [JsonIgnore] [ProtoMember(3)] public byte[] Body { get; set; }

        public override string ToString() => UtilityAOT.Json.ToJson(this);
    }
}
