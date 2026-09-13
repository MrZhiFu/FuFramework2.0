using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
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

                var list = MessageHandlerDictionary.GetOrAdd(messageHandlerAttribute.MessageType, static _ => new List<MessageHandlerAttribute>(8));

                if (ContainsHandler(list, messageHandler, messageHandlerAttribute.InvokeMethodName))
                {
                    FuLogger.LogError("重复注册消息处理器：" + type.FullName + "->" + methodInfo.Name);
                    continue;
                }

                list.Add(messageHandlerAttribute);
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

                if (!MessageHandlerDictionary.TryGetValue(messageHandlerAttribute.MessageType, out var list) || list == null)
                {
                    FuLogger.LogError("未找到消息处理器：" + type.FullName + "->" + methodInfo.Name);
                    continue;
                }

                // 以 (处理对象实例, 方法名) 显式比对：GetCustomAttribute 每次都返回新实例、
                // MessageHandlerAttribute 未重写 Equals，用 List.Contains/Remove 会永远判不中，
                // 原实现因此会退化为 TryRemove(messageType) 摘掉该类型的全部处理器。
                var removed = false;
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var exist = list[i];
                    if (!ReferenceEquals(exist.TargetHandler, messageHandler)) continue;
                    if (exist.InvokeMethodName != messageHandlerAttribute.InvokeMethodName) continue;

                    list.RemoveAt(i);
                    removed = true;
                }

                if (!removed)
                {
                    FuLogger.LogError("未找到消息处理器：" + type.FullName + "->" + methodInfo.Name);
                    continue;
                }

                if (list.Count <= 0)
                {
                    MessageHandlerDictionary.TryRemove(messageHandlerAttribute.MessageType, out _);
                }
            }
        }

        /// <summary>
        /// 判断列表中是否已注册了同一处理对象上的同一方法
        /// </summary>
        private static bool ContainsHandler(List<MessageHandlerAttribute> list, IMessageHandler messageHandler, string invokeMethodName)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var exist = list[i];
                if (!ReferenceEquals(exist.TargetHandler, messageHandler)) continue;
                if (exist.InvokeMethodName != invokeMethodName) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取消息处理器，并复制到调用方提供的复用列表中。
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="destination">调用方提供的复用列表，会被先清空</param>
        internal static void GetHandlers(Type messageType, List<MessageHandlerAttribute> destination)
        {
            destination.Clear();
            if (MessageHandlerDictionary.TryGetValue(messageType, out var list) && list != null)
            {
                // 复制到调用方缓冲区：派发期间用户代码可能注册/注销处理器，
                // 直接遍历内部列表会抛 InvalidOperationException。
                destination.AddRange(list);
                return;
            }

            FuLogger.LogWarning("没有找到消息处理器消息类型：" + messageType.Name);
        }
    }
}
