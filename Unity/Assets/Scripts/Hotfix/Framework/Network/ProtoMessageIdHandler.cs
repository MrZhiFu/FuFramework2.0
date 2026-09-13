using System;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 协议消息处理器。
    ///
    /// 说明（项目铁律 4：运行时杜绝反射）：
    ///     消息ID &lt;-&gt; 类型的映射原先由 <c>Assembly.GetTypes()</c> 全程序集扫描 + 读取
    ///     <c>MessageTypeHandlerAttribute</c> 特性构建，现改为消费生成期固化的静态注册表：
    ///     生成物 <c>Generated/ProtoMessageRegistry.g.cs</c>，生成脚本 <c>Protobuf/gen-proto-registry.py</c>。
    ///     proto 变更（新增/删除消息、改ID、改接口）后必须重新运行生成脚本。
    ///
    ///     数据类型与查询语义由 <see cref="MessageIdRegistry"/> 持有，本类保持原有对外 API 不变。
    /// </summary>
    public static class ProtoMessageIdHandler
    {
        private static bool IsInitialized;

        /// <summary>
        /// 根据消息ID获取请求的类型
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>请求的类型</returns>
        public static Type GetReqTypeById(int messageId)
        {
            if (MessageIdRegistry.ReqCount <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return null;
            }

            return MessageIdRegistry.GetReqTypeById(messageId);
        }

        /// <summary>
        /// 根据类型获取请求消息ID
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>请求消息ID</returns>
        public static int GetReqMessageIdByType(Type type)
        {
            if (MessageIdRegistry.ReqCount <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return 0;
            }

            return MessageIdRegistry.GetReqMessageIdByType(type);
        }

        /// <summary>
        /// 根据消息ID获取响应的类型
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>响应的类型</returns>
        public static Type GetRespTypeById(int messageId)
        {
            if (MessageIdRegistry.RespCount <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return null;
            }

            return MessageIdRegistry.GetRespTypeById(messageId);
        }

        /// <summary>
        /// 根据类型获取响应消息ID
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>响应消息ID</returns>
        public static int GetRespMessageIdByType(Type type)
        {
            if (MessageIdRegistry.RespCount <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return 0;
            }

            return MessageIdRegistry.GetRespMessageIdByType(type);
        }

        /// <summary>
        /// 获取消息类型是否是心跳类型
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <returns></returns>
        public static bool IsHeartbeat(Type type) => MessageIdRegistry.IsHeartbeat(type);

        /// <summary>
        /// 初始化所有协议对象。
        /// 说明：注册数据来自生成物 <c>ProtoMessageRegistry</c>（编译期固化），
        /// 不再扫描程序集、不再读取特性，故**无需任何程序集参数**。
        /// </summary>
        public static void Init()
        {
            if (IsInitialized) return;

            // 与原实现一致：先清空，使「注册失败后重试」不会残留半成品状态。
            MessageIdRegistry.Reset();

            // 生成期固化的静态注册表（消息ID映射 / 心跳集合 / 用户消息处理方法清单）。
            ProtoMessageRegistry.RegisterAll();

            // 扫描全部成功后才标记初始化完成，避免注册抛出重复ID异常后残留半成品状态。
            IsInitialized = true;
        }
    }
}
