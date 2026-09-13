using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 用户消息处理方法的登记项（由生成物 <c>ProtoMessageRegistry</c> 填充）。
    /// 说明：持有的是**生成期产出的直接委托**，不再持有方法名 / MethodInfo，
    /// 因此运行期无需按名字查找方法，也不存在任何反射。
    /// </summary>
    public readonly struct ProtoMessageHandlerMethod
    {
        /// <summary>
        /// [MessageHandler] 声明的消息类型。
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// [MessageHandler] 声明的方法名。仅用于日志与去重比对，不参与方法查找。
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// 生成期产出的直接委托，形如
        /// <c>static (handler, message) =&gt; ((BagManager)handler).OnX((X)message)</c>。
        /// </summary>
        public Action<IMessageHandler, MessageObject> Invoke { get; }

        public ProtoMessageHandlerMethod(Type messageType, string methodName, Action<IMessageHandler, MessageObject> invoke)
        {
            MessageType = messageType;
            MethodName  = methodName;
            Invoke      = invoke;
        }
    }

    /// <summary>
    /// 协议消息处理帮助类
    ///
    /// 说明（项目铁律 4：运行时杜绝反射）：
    ///     「消息处理对象类型 -&gt; 其 [MessageHandler] 方法清单」原先通过
    ///     <c>Type.GetMethods</c> + <c>MethodInfo.GetCustomAttribute</c> 在注册时读取特性获得，
    ///     现改由生成期扫描源码产出的静态表提供（生成脚本 Protobuf/gen-proto-registry.py）：
    ///     生成物直接产出**强类型委托**，注册时仅做绑定，运行期零反射
    ///     （无 GetMethods / IsDefined / GetCustomAttribute / CreateDelegate / MethodInfo.Invoke）。
    ///     新增 [MessageHandler] 方法后必须重新运行生成脚本，否则会记录错误日志且该处理器不生效。
    /// </summary>
    public static class ProtoMessageHandler
    {
        /// <summary>
        /// 消息处理器字典, Key为消息类型, Value为消息处理器列表
        /// </summary>
        private static readonly ConcurrentDictionary<Type, List<MessageHandlerAttribute>> MessageHandlerDictionary = new();

        /// <summary>
        /// 「消息处理对象类型 -&gt; 其 [MessageHandler] 方法清单」静态表（由生成物写入）。
        /// 说明：仅在启动期 ProtoMessageIdHandler.Init 内写入一次，之后只读；
        /// 仍使用 ConcurrentDictionary 以容忍 Add/Remove 与初始化并发的极端情况。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ProtoMessageHandlerMethod[]> HandlerMethodRegistry = new();

        /// <summary>
        /// 登记某个消息处理对象类型的 [MessageHandler] 方法清单。由生成物调用。
        /// 说明：无 [MessageHandler] 方法的 IMessageHandler 实现类型也会被登记为空清单，
        /// 以便 <see cref="Add"/> 能区分「已登记但无处理方法」与「未登记（需重新生成）」。
        /// </summary>
        /// <param name="handlerType">消息处理对象类型</param>
        /// <param name="methods">该类型上的 [MessageHandler] 方法清单</param>
        internal static void RegisterHandlerType(Type handlerType, ProtoMessageHandlerMethod[] methods)
        {
            handlerType.NotNull(nameof(handlerType));
            HandlerMethodRegistry[handlerType] = methods ?? Array.Empty<ProtoMessageHandlerMethod>();
        }

        /// <summary>
        /// 增加消息处理器
        /// </summary>
        /// <param name="messageHandler">消息接收对象</param>
        public static void Add(IMessageHandler messageHandler)
        {
            messageHandler.NotNull(nameof(messageHandler));
            var type = messageHandler.GetType();

            if (!HandlerMethodRegistry.TryGetValue(type, out var registeredMethods))
            {
                FuLogger.LogError("消息处理对象类型未在生成的注册表中：" + type.FullName +
                                  "，请重新运行 Protobuf/Proto2CsExport-All.bat 后再试");
                return;
            }

            for (var i = 0; i < registeredMethods.Length; i++)
            {
                var entry = registeredMethods[i];

                if (entry.Invoke == null)
                {
                    FuLogger.LogError("生成注册表中的处理委托为空：" + type.FullName + "->" + entry.MethodName +
                                      "，请重新运行 Protobuf/Proto2CsExport-All.bat 后再试");
                    continue;
                }

                var list = MessageHandlerDictionary.GetOrAdd(entry.MessageType, static _ => new List<MessageHandlerAttribute>(8));

                if (ContainsHandler(list, messageHandler, entry.MethodName))
                {
                    FuLogger.LogError("重复注册消息处理器：" + type.FullName + "->" + entry.MethodName);
                    continue;
                }

                // 绑定生成期产出的强类型委托：运行期不再查找方法、不再构造委托。
                var messageHandlerAttribute = new MessageHandlerAttribute(entry.MessageType, entry.MethodName);
                messageHandlerAttribute.Bind(messageHandler, entry.Invoke);
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

            if (!HandlerMethodRegistry.TryGetValue(type, out var registeredMethods))
            {
                FuLogger.LogError("消息处理对象类型未在生成的注册表中：" + type.FullName +
                                  "，请重新运行 Protobuf/Proto2CsExport-All.bat 后再试");
                return;
            }

            for (var i = 0; i < registeredMethods.Length; i++)
            {
                var entry = registeredMethods[i];

                if (!MessageHandlerDictionary.TryGetValue(entry.MessageType, out var list) || list == null)
                {
                    FuLogger.LogError("未找到消息处理器：" + type.FullName + "->" + entry.MethodName);
                    continue;
                }

                // 以 (处理对象实例, 方法名) 显式比对：两处信息都由生成表提供，
                // 不依赖 Attribute 实例的引用相等性（MessageHandlerAttribute 未重写 Equals）。
                var removed = false;
                for (var j = list.Count - 1; j >= 0; j--)
                {
                    var exist = list[j];
                    if (!ReferenceEquals(exist.TargetHandler, messageHandler)) continue;
                    if (exist.InvokeMethodName != entry.MethodName) continue;

                    list.RemoveAt(j);
                    removed = true;
                }

                if (!removed)
                {
                    FuLogger.LogError("未找到消息处理器：" + type.FullName + "->" + entry.MethodName);
                    continue;
                }

                if (list.Count <= 0)
                {
                    MessageHandlerDictionary.TryRemove(entry.MessageType, out _);
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
