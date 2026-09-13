using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 协议消息种类。
    /// 说明：一个消息类型可能同时是「请求」与「心跳」（例如 ReqHeartBeat），故用标志位表达。
    ///      为与原实现保持一致，「请求 / 响应 / 推送」三个互斥位在生成期已按
    ///      Request &gt; Response &gt; Notify 的优先级归一，不会同时出现。
    /// </summary>
    [Flags]
    public enum EMessageKind
    {
        /// <summary>
        /// 未实现任何消息接口（原实现同样不会注册）。
        /// </summary>
        None = 0,

        /// <summary>
        /// 请求消息（IRequestMessage）
        /// </summary>
        Request = 1 << 0,

        /// <summary>
        /// 响应消息（IResponseMessage）
        /// </summary>
        Response = 1 << 1,

        /// <summary>
        /// 推送消息（INotifyMessage）
        /// </summary>
        Notify = 1 << 2,

        /// <summary>
        /// 心跳消息（IHeartBeatMessage）
        /// </summary>
        HeartBeat = 1 << 3,
    }

    /// <summary>
    /// 协议消息注册表。
    /// 功能：
    ///     1. 记录「消息ID &lt;-&gt; 消息类型」的双向映射（请求表 / 响应表）。
    ///     2. 记录心跳消息类型集合。
    ///
    /// 说明（项目铁律 4）：注册数据不再通过运行时扫描程序集 + 读取特性获得，
    /// 而是由生成物 <c>Generated/ProtoMessageRegistry.g.cs</c> 静态调用 <see cref="Register{T}"/> 写入。
    /// 生成脚本：<c>Protobuf/gen-proto-registry.py</c>。
    /// </summary>
    public static class MessageIdRegistry
    {
        /// <summary>
        /// 请求消息：消息ID &lt;-&gt; 类型
        /// </summary>
        private static readonly FuBidirectionalDictionary<int, Type> ReqDictionary = new();

        /// <summary>
        /// 响应/推送消息：消息ID &lt;-&gt; 类型
        /// （原实现把 IResponseMessage 与 INotifyMessage 一并放入同一张表，这里保持一致）
        /// </summary>
        private static readonly FuBidirectionalDictionary<int, Type> RespDictionary = new();

        /// <summary>
        /// 心跳消息类型集合。
        /// 说明：IsHeartbeat 在每发一个包时都会被调用（默认包头处理器），
        /// 使用 HashSet 避免线性查找开销。
        /// </summary>
        private static readonly HashSet<Type> HeartBeatTypes = new();

        /// <summary>
        /// 请求消息数量。
        /// </summary>
        public static int ReqCount => ReqDictionary.Count;

        /// <summary>
        /// 响应/推送消息数量。
        /// </summary>
        public static int RespCount => RespDictionary.Count;

        /// <summary>
        /// 清空全部注册数据（供初始化重试使用）。
        /// </summary>
        public static void Reset()
        {
            ReqDictionary.Clear();
            RespDictionary.Clear();
            HeartBeatTypes.Clear();
        }

        /// <summary>
        /// 注册一个协议消息类型。
        /// 错误语义与原反射实现完全一致：重复的心跳类型 / 重复的消息ID 直接抛异常。
        /// </summary>
        /// <param name="messageId">消息ID（模块 &lt;&lt; 16 | 序号）</param>
        /// <param name="kind">消息种类（可含心跳位）</param>
        /// <typeparam name="T">协议消息类型</typeparam>
        public static void Register<T>(int messageId, EMessageKind kind) where T : MessageObject
        {
            var type = typeof(T);

            if ((kind & EMessageKind.HeartBeat) != 0)
            {
                if (!HeartBeatTypes.Add(type)) throw new InvalidOperationException($"心跳消息重复==>类型:{type.FullName}");
            }

            if ((kind & EMessageKind.Request) != 0)
            {
                // 请求
                if (ReqDictionary.TryAdd(messageId, type)) return;
                ReqDictionary.TryGetValueByKey(messageId, out var exist);
                throw new InvalidOperationException($"请求Id重复==>当前ID:{messageId},已有ID类型:{exist?.FullName ?? type.FullName}");
            }

            if ((kind & (EMessageKind.Response | EMessageKind.Notify)) != 0)
            {
                // 返回
                if (RespDictionary.TryAdd(messageId, type)) return;
                RespDictionary.TryGetValueByKey(messageId, out var exist);
                throw new InvalidOperationException($"返回Id重复==>当前ID:{messageId},已有ID类型:{exist?.FullName ?? type.FullName}");
            }
        }

        /// <summary>
        /// 根据消息ID获取请求的类型（未注册时返回 null）。
        /// </summary>
        public static Type GetReqTypeById(int messageId)
        {
            ReqDictionary.TryGetValueByKey(messageId, out var value);
            return value;
        }

        /// <summary>
        /// 根据类型获取请求消息ID（未注册时返回 0）。
        /// </summary>
        public static int GetReqMessageIdByType(Type type)
        {
            ReqDictionary.TryGetKeyByValue(type, out var value);
            return value;
        }

        /// <summary>
        /// 根据消息ID获取响应/推送的类型（未注册时返回 null）。
        /// </summary>
        public static Type GetRespTypeById(int messageId)
        {
            RespDictionary.TryGetValueByKey(messageId, out var value);
            return value;
        }

        /// <summary>
        /// 根据类型获取响应/推送消息ID（未注册时返回 0）。
        /// </summary>
        public static int GetRespMessageIdByType(Type type)
        {
            RespDictionary.TryGetKeyByValue(type, out var value);
            return value;
        }

        /// <summary>
        /// 获取消息类型是否是心跳类型。
        /// </summary>
        public static bool IsHeartbeat(Type type) => type != null && HeartBeatTypes.Contains(type);
    }
}
