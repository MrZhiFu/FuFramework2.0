using System;
using System.Collections.Generic;
using System.Reflection;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Extension;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 协议消息处理器
    /// </summary>
    public static class ProtoMessageIdHandler
    {
        private static readonly FuBidirectionalDictionary<int, Type> ReqDictionary  = new();
        private static readonly FuBidirectionalDictionary<int, Type> RespDictionary = new();

        /// <summary>
        /// 心跳消息类型集合。
        /// 说明：IsHeartbeat 在每发一个包时都会被调用（默认包头处理器），
        /// 使用 HashSet 避免原 List 的线性查找开销。
        /// </summary>
        private static readonly HashSet<Type> HeartBeatList = new();

        private static bool IsInitialized = false;

        /// <summary>
        /// 根据消息ID获取请求的类型
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>请求的类型</returns>
        public static Type GetReqTypeById(int messageId)
        {
            if (ReqDictionary.Count <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return null;
            }

            ReqDictionary.TryGetValueByKey(messageId, out var value);
            return value;
        }

        /// <summary>
        /// 根据类型获取请求消息ID
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>请求消息ID</returns>
        public static int GetReqMessageIdByType(Type type)
        {
            if (ReqDictionary.Count <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return 0;
            }

            ReqDictionary.TryGetKeyByValue(type, out var value);
            return value;
        }

        /// <summary>
        /// 根据消息ID获取响应的类型
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>响应的类型</returns>
        public static Type GetRespTypeById(int messageId)
        {
            if (RespDictionary.Count <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return null;
            }

            RespDictionary.TryGetValueByKey(messageId, out var value);
            return value;
        }

        /// <summary>
        /// 根据类型获取响应消息ID
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>响应消息ID</returns>
        public static int GetRespMessageIdByType(Type type)
        {
            if (RespDictionary.Count <= 0)
            {
                FuLogger.LogWarning("请先确认是否初始化 调用 ProtoMessageIdHandler.Init()");
                return 0;
            }

            RespDictionary.TryGetKeyByValue(type, out var value);
            return value;
        }

        /// <summary>
        /// 获取消息类型是否是心跳类型
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <returns></returns>
        public static bool IsHeartbeat(Type type) => HeartBeatList.Contains(type);

        /// <summary>
        /// 初始化所有协议对象。
        /// 注意（铁律 4 降级说明）：受「不引入代码生成器」的约束，这里的消息类型发现仍基于反射
        /// （assembly.GetTypes + 特性读取），但仅在启动时执行一次，且先用 IsDefined 过滤，
        /// 只有真正带 MessageTypeHandlerAttribute 的类型才会实例化特性对象。
        /// 彻底方案应由代码生成器产出「消息ID ↔ 类型」静态映射表。
        /// </summary>
        public static void Init(Assembly assembly)
        {
            if (IsInitialized) return;

            ReqDictionary.Clear();
            RespDictionary.Clear();
            HeartBeatList.Clear();

            if (assembly == null)
            {
                FuLogger.LogError("[ProtoMessageIdHandler] 初始化失败：assembly 为空。");
                return;
            }

            var types = assembly.GetTypes();
            // StringBuilder stringBuilder = new StringBuilder(1024);
            foreach (var type in types)
            {
                // IsDefined 不创建特性实例，比 GetCustomAttribute 更省；绝大多数类型会被这里直接跳过。
                if (!type.IsDefined(typeof(MessageTypeHandlerAttribute), false)) continue;

                var attribute = type.GetCustomAttribute(typeof(MessageTypeHandlerAttribute));

                // stringBuilder.AppendLine(type.FullName);
                if (attribute is MessageTypeHandlerAttribute messageIdHandler)
                {
                    if (type.IsImplWithInterface(typeof(IHeartBeatMessage)))
                    {
                        if (!HeartBeatList.Add(type)) throw new InvalidOperationException($"心跳消息重复==>类型:{type.FullName}");
                    }

                    if (type.IsImplWithInterface(typeof(IRequestMessage)))
                    {
                        // 请求
                        if (ReqDictionary.TryAdd(messageIdHandler.MessageId, type)) continue;
                        ReqDictionary.TryGetValueByKey(messageIdHandler.MessageId, out var value);
                        throw new InvalidOperationException($"请求Id重复==>当前ID:{messageIdHandler.MessageId},已有ID类型:{value.FullName}");
                    }

                    if (type.IsImplWithInterface(typeof(IResponseMessage)))
                    {
                        // 返回
                        if (RespDictionary.TryAdd(messageIdHandler.MessageId, type)) continue;
                        RespDictionary.TryGetValueByKey(messageIdHandler.MessageId, out var value);
                        throw new InvalidOperationException($"返回Id重复==>当前ID:{messageIdHandler.MessageId},已有ID类型:{value.FullName}");
                    }

                    if (type.IsImplWithInterface(typeof(INotifyMessage)))
                    {
                        // 返回
                        if (RespDictionary.TryAdd(messageIdHandler.MessageId, type)) continue;
                        RespDictionary.TryGetValueByKey(messageIdHandler.MessageId, out var value);
                        throw new InvalidOperationException($"返回Id重复==>当前ID:{messageIdHandler.MessageId},已有ID类型:{value.FullName}");
                    }
                }
            }

            // 扫描全部成功后才标记初始化完成，避免注册抛出重复ID异常后残留半成品状态。
            IsInitialized = true;

            // GameFrameworkLog.Debug(" 注册消息ID类型: " + stringBuilder);
            // GameFrameworkLog.Info(" 注册消息ID类型: 结束");
        }
    }
}
