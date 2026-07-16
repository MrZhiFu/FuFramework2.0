using Newtonsoft.Json;
using Utility = FuFramework.Core.Runtime.Utility;
using UtilityAOT = FuFramework.Core.Runtime.UtilityAOT;
using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace Hotfix.Network
{
    /// <summary>
    /// 消息基类
    /// </summary>
    [ProtoContract]
    public class MessageObject
    {
        /// <summary>
        /// 消息唯一编号
        /// </summary>
        [JsonIgnore]
        public int UniqueId { get; private set; }

        protected MessageObject() => UpdateUniqueId();

        /// <summary>
        /// 更新唯一编码
        /// </summary>
        public void UpdateUniqueId() => UniqueId = Utility.IdGenerator.GetNextUniqueIntId();

        /// <summary>
        /// 设置唯一编码
        /// </summary>
        public void SetUpdateUniqueId(int uniqueId) => UniqueId = uniqueId;

        public override string ToString() => UtilityAOT.Json.ToJson(this);
    }
}