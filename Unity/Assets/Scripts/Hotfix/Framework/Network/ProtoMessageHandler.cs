using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using FuFramework.Core.Runtime;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Network
{
    /// <summary>
    /// 协议消息处理帮助类
    /// </summary>
    public static class ProtoMessageHandler
    {
        /// <summary>
        /// 消息处理器字典, Key为消息类型, Value为消息处理器列表
        /// </summary>
        private static readonly ConcurrentDictionary<Type, List<MessageHandlerAttribute>> MessageHandlerDictionary = new();

        /// <summary>
        /// 空消息处理器列表, 避免每次都新建一个空列表
        /// </summary>
        private static readonly List<MessageHandlerAttribute> EmptyList = new();

        /// <summary>
        /// 增加消息处理器
        /// </summary>
        /// <param name="messageHandler">消息接收对象</param>
        public static void Add(IMessageHandler messageHandler)
        {
            messageHandler.NotNull(nameof(messageHandler));
            var type        = messageHandler.GetType();
            var methodInfos = type.GetMethods(MessageHandlerAttribute.Flags);

            foreach (var methodInfo in methodInfos)
            {
                var messageHandlerAttribute = methodInfo.GetCustomAttribute<MessageHandlerAttribute>();
                if (messageHandlerAttribute == null) continue;

                var isAddSuccess = messageHandlerAttribute.Add(messageHandler);
                if (!isAddSuccess)
                {
                    FuLogger.LogError("初始化消息处理器：" + type.FullName + "->" + methodInfo.Name + " 失败");
                    continue;
                }

                MessageHandlerDictionary.TryGetValue(messageHandlerAttribute.MessageType, out var list);
                if (list == null)
                {
                    list = new List<MessageHandlerAttribute>(8);
                    MessageHandlerDictionary.TryAdd(messageHandlerAttribute.MessageType, list);
                }

                if (!list.Contains(messageHandlerAttribute))
                    list.Add(messageHandlerAttribute);
                else
                    FuLogger.LogError("重复注册消息处理器：" + type.FullName + "->" + methodInfo.Name);
            }
        }

        /// <summary>
        /// 移除消息处理器
        /// </summary>
        /// <param name="messageHandler">消息接收对象</param>
        public static void Remove(IMessageHandler messageHandler)
        {
            messageHandler.NotNull(nameof(messageHandler));
            var type = messageHandler.GetType();

            var methodInfos = type.GetMethods(MessageHandlerAttribute.Flags);

            foreach (var methodInfo in methodInfos)
            {
                var messageHandlerAttribute = methodInfo.GetCustomAttribute<MessageHandlerAttribute>();
                if (messageHandlerAttribute == null) continue;

                var isRemoveSuccess = messageHandlerAttribute.Remove(messageHandler);
                if (!isRemoveSuccess)
                {
                    FuLogger.LogError("移除消息处理器：" + type.FullName + "->" + methodInfo.Name + " 失败");
                    continue;
                }

                var isFind = MessageHandlerDictionary.TryGetValue(messageHandlerAttribute.MessageType, out var list);
                if (isFind)
                {
                    if (list?.Contains(messageHandlerAttribute) == true)
                    {
                        list.Remove(messageHandlerAttribute);
                        if (list.Count > 0) continue;
                    }

                    MessageHandlerDictionary.TryRemove(messageHandlerAttribute.MessageType, out _);
                    continue;
                }

                FuLogger.LogError("未找到消息处理器：" + type.FullName + "->" + methodInfo.Name);
            }
        }


        /// <summary>
        /// 获取消息处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <returns>消息处理器</returns>
        internal static List<MessageHandlerAttribute> GetHandlers(Type messageType)
        {
            if (MessageHandlerDictionary.TryGetValue(messageType, out var list))
                return list == null ? EmptyList : new List<MessageHandlerAttribute>(list);
            FuLogger.LogWarning("没有找到消息处理器消息类型：" + messageType.Name);
            return EmptyList;
        }
    }
}