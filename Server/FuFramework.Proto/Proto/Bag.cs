using System;
using ProtoBuf;
using System.Collections.Generic;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.Messages;

namespace FuFramework.Proto.Proto
{
    /// <summary>
    /// 背包道具定义
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("背包道具定义")]
    public sealed class BagItem
    {
        /// <summary>
        /// 道具id
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("道具id")]
        public int ItemId { get; set; }

        /// <summary>
        /// 道具数量
        /// </summary>
        [ProtoMember(2)]
        [System.ComponentModel.Description("道具数量")]
        public long Count { get; set; }
    }

    /// <summary>
    /// 请求背包数据
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("请求背包数据")]
    [MessageId(MessageIds.Bag.ReqBagInfo)]
    public sealed class ReqBagInfo : MessageObject, IRequestMessage
    {
    }

    /// <summary>
    /// 背包数据响应
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("背包数据响应")]
    [MessageId(MessageIds.Bag.RespBagInfo)]
    public sealed class RespBagInfo : MessageObject, IResponseMessage
    {
        /// <summary>
        /// 道具字典，key:道具id，value:道具数量
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("道具字典，key:道具id，value:道具数量")]
        [ProtoMap(DisableMap = true)]
        public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

        /// <summary>
        /// 返回的错误码
        /// </summary>
        [ProtoMember(2047)]
        [System.ComponentModel.Description("返回的错误码")]
        public int ErrorCode { get; set; }
    }

    /// <summary>
    /// 请求使用道具
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("请求使用道具")]
    [MessageId(MessageIds.Bag.ReqUseItem)]
    public sealed class ReqUseItem : MessageObject, IRequestMessage
    {
        /// <summary>
        /// 道具id
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("道具id")]
        public int ItemId { get; set; }

        /// <summary>
        /// 使用数量
        /// </summary>
        [ProtoMember(2)]
        [System.ComponentModel.Description("使用数量")]
        public long Count { get; set; }
    }

    /// <summary>
    /// 使用道具响应
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("使用道具响应")]
    [MessageId(MessageIds.Bag.RespUseItem)]
    public sealed class RespUseItem : MessageObject, IResponseMessage
    {
        /// <summary>
        /// 道具id
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("道具id")]
        public int ItemId { get; set; }

        /// <summary>
        /// 使用后剩余的道具数量
        /// </summary>
        [ProtoMember(2)]
        [System.ComponentModel.Description("使用后剩余的道具数量")]
        public long Count { get; set; }

        /// <summary>
        /// 返回的错误码
        /// </summary>
        [ProtoMember(2047)]
        [System.ComponentModel.Description("返回的错误码")]
        public int ErrorCode { get; set; }
    }

    /// <summary>
    /// 请求增加道具
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("请求增加道具")]
    [MessageId(MessageIds.Bag.ReqAddItem)]
    public sealed class ReqAddItem : MessageObject, IRequestMessage
    {
        /// <summary>
        /// 增加的道具，key:道具id，value:增加数量
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("增加的道具，key:道具id，value:增加数量")]
        [ProtoMap(DisableMap = true)]
        public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();
    }

    /// <summary>
    /// 增加道具响应
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("增加道具响应")]
    [MessageId(MessageIds.Bag.RespAddItem)]
    public sealed class RespAddItem : MessageObject, IResponseMessage
    {
        /// <summary>
        /// 变化的道具，key:道具id，value:数量
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("变化的道具，key:道具id，value:数量")]
        [ProtoMap(DisableMap = true)]
        public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

        /// <summary>
        /// 返回的错误码
        /// </summary>
        [ProtoMember(2047)]
        [System.ComponentModel.Description("返回的错误码")]
        public int ErrorCode { get; set; }
    }

    /// <summary>
    /// 请求减少道具
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("请求减少道具")]
    [MessageId(MessageIds.Bag.ReqRemoveItem)]
    public sealed class ReqRemoveItem : MessageObject, IRequestMessage
    {
        /// <summary>
        /// 减少的道具，key:道具id，value:减少数量
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("减少的道具，key:道具id，value:减少数量")]
        [ProtoMap(DisableMap = true)]
        public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();
    }

    /// <summary>
    /// 减少道具响应
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("减少道具响应")]
    [MessageId(MessageIds.Bag.RespRemoveItem)]
    public sealed class RespRemoveItem : MessageObject, IResponseMessage
    {
        /// <summary>
        /// 变化的道具，key:道具id，value:数量
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("变化的道具，key:道具id，value:数量")]
        [ProtoMap(DisableMap = true)]
        public Dictionary<int, long> ItemDic { get; set; } = new Dictionary<int, long>();

        /// <summary>
        /// 返回的错误码
        /// </summary>
        [ProtoMember(2047)]
        [System.ComponentModel.Description("返回的错误码")]
        public int ErrorCode { get; set; }
    }

    /// <summary>
    /// 单个道具变化通知
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("单个道具变化通知")]
    [MessageId(MessageIds.Bag.NotifyBagItem)]
    public sealed class NotifyBagItem : MessageObject, INotifyMessage
    {
        /// <summary>
        /// 道具id
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("道具id")]
        public int ItemId { get; set; }

        /// <summary>
        /// 最终道具数量
        /// </summary>
        [ProtoMember(2)]
        [System.ComponentModel.Description("最终道具数量")]
        public long Count { get; set; }

        /// <summary>
        /// 变化的值
        /// </summary>
        [ProtoMember(3)]
        [System.ComponentModel.Description("变化的值")]
        public long Value { get; set; }
    }

    /// <summary>
    /// 背包数据变化通知
    /// </summary>
    [ProtoContract]
    [System.ComponentModel.Description("背包数据变化通知")]
    [MessageId(MessageIds.Bag.NotifyBagInfoChanged)]
    public sealed class NotifyBagInfoChanged : MessageObject, INotifyMessage
    {
        /// <summary>
        /// 变化的道具，key:道具id，value:变化信息
        /// </summary>
        [ProtoMember(1)]
        [System.ComponentModel.Description("变化的道具，key:道具id，value:变化信息")]
        [ProtoMap(DisableMap = true)]
        public Dictionary<int, NotifyBagItem> ItemDic { get; set; } = new Dictionary<int, NotifyBagItem>();
    }

}
