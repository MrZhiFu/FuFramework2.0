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
    /// 网络非RPC返回消息处理器属性定义。
    ///
    /// 说明（项目铁律 4：运行时杜绝反射）：
    ///     「方法发现」（<c>Type.GetMethods</c> + <c>MethodInfo.IsDefined</c>）属于本类的残留反射：
    ///     用户方法可能是游戏侧类型的私有方法，生成期无法为其构造委托，故仍按方法名定位 MethodInfo。
    ///     但「强类型委托构造」已改为消费生成物 <c>Generated/ProtoMessageRegistry.g.cs</c>
    ///     （见 <see cref="MessageDelegateFactory.Create"/>），不再有 <c>MakeGenericMethod</c> 反射调用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        public const BindingFlags Flags = BindingFlags.Default   |
                                          BindingFlags.Public    |
                                          BindingFlags.NonPublic |
                                          BindingFlags.Instance  |
                                          BindingFlags.Static;

        /// <summary>
        /// 单个方法允许积压的最大未处理消息数量，防止消息队列无界增长。
        /// </summary>
        private const int MaxQueuedMessageCount = 256;

        /// <summary>
        /// 消息对象
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// 执行的方法名称
        /// </summary>
        private readonly string m_InvokeMethodName;

        /// <summary>
        /// 执行的方法
        /// </summary>
        private MethodInfo m_InvokeMethod;

        /// <summary>
        /// 消息处理器
        /// </summary>
        private IMessageHandler m_MessageHandler;

        /// <summary>
        /// 已缓存的消息处理委托。
        /// 注册阶段一次性创建，收包阶段直接调用，避免每包执行 MethodInfo.Invoke 反射。
        /// </summary>
        private Action<IMessageHandler, MessageObject> m_InvokeDelegate;

        /// <summary>
        /// 消息处理对象队列
        /// </summary>
        private readonly Queue<MessageObject> m_MessageObjects = new();

        /// <summary>
        /// 网络消息处理器
        /// </summary>
        /// <param name="message">注册的消息对象。需要继承MessageObject和实现IResponseMessage</param>
        /// <param name="invokeMethodName">执行的方法名称。建议使用nameof标记当前的函数</param>
        public MessageHandlerAttribute(Type message, string invokeMethodName)
        {
            message.NotNull(nameof(message));
            invokeMethodName.NotNullOrEmpty(nameof(invokeMethodName));
            m_InvokeMethodName = invokeMethodName;
            if (message.BaseType != typeof(MessageObject))
                throw new ArgumentException("message必须继承:" + nameof(MessageObject));

            if (!message.IsImplWithInterface(typeof(INotifyMessage)))
                throw new ArgumentException($"message:{message.FullName}必须实现:" + nameof(INotifyMessage));

            MessageType = message;
        }

        /// <summary>
        /// 注册时匹配到的方法
        /// </summary>
        internal MethodInfo InvokeMethod => m_InvokeMethod;

        /// <summary>
        /// 注册时匹配到的方法名（用于显式比对，避免依赖 Attribute 的引用相等性）
        /// </summary>
        internal string InvokeMethodName => m_InvokeMethodName;

        /// <summary>
        /// 注册的处理对象实例（用于显式比对）
        /// </summary>
        internal IMessageHandler TargetHandler => m_MessageHandler;

        /// <summary>
        /// 设置消息对象
        /// </summary>
        /// <param name="messageObject">消息对象</param>
        public void SetMessageObject(MessageObject messageObject)
        {
            messageObject.NotNull(nameof(messageObject));
            if (m_MessageObjects.Count >= MaxQueuedMessageCount)
            {
                // 队列无界会导致内存持续增长（例如处理函数持续失败时），
                // 这里丢弃最旧的一条，保证积压有上限。
                FuLogger.LogWarning($"消息处理队列已满({MaxQueuedMessageCount})，丢弃最旧消息。方法：{m_InvokeMethodName}");
                m_MessageObjects.Dequeue();
            }

            m_MessageObjects.Enqueue(messageObject);
        }

        internal void Invoke()
        {
            if (m_MessageObjects.Count <= 0)
            {
                FuLogger.LogWarning($"没有消息对象转发到方法：{m_InvokeMethodName}");
                return;
            }

            // 先出队再处理：处理过程中抛异常时消息不会残留在队列里造成无界堆积。
            var messageObject = m_MessageObjects.Dequeue();

            if (m_InvokeDelegate == null)
                throw new ArgumentNullException(nameof(m_InvokeDelegate), $"未找到方法：{m_InvokeMethodName}.请确认是否注册成功");

            m_InvokeDelegate(m_MessageHandler, messageObject);
        }

        /// <summary>
        /// 增加消息处理器
        /// </summary>
        /// <param name="messageHandler">消息处理器对象</param>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal bool Add(IMessageHandler messageHandler)
        {
            MessageType.NotNull(   nameof(MessageType));
            messageHandler.NotNull(nameof(messageHandler));
            m_MessageHandler = messageHandler;
            var target = messageHandler.GetType();

            var methodInfos = target.GetMethods(Flags);

            foreach (var method in methodInfos)
            {
                if (!method.IsDefined(typeof(MessageHandlerAttribute), true)) continue;
                if (method.Name != m_InvokeMethodName) continue;

                if (method.GetParameters().Length != 1)
                    throw new TargetParameterCountException("参数个数必须为1");

                if (method.GetParameters()[0].ParameterType.FullName != MessageType.FullName)
                    throw new ArgumentException("参数类型数必须为:" + MessageType.FullName);

                m_InvokeMethod   = method;
                // 注册阶段一次性创建强类型委托并缓存，收包阶段不再有反射调用开销。
                m_InvokeDelegate = CreateInvokeDelegate(method, messageHandler);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 删除消息处理器
        /// </summary>
        /// <param name="messageHandler">消息处理器对象</param>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal bool Remove(IMessageHandler messageHandler)
        {
            MessageType.NotNull(   nameof(MessageType));
            messageHandler.NotNull(nameof(messageHandler));
            m_MessageHandler = null;
            m_InvokeDelegate = null;
            var target = messageHandler.GetType();

            var methodInfos = target.GetMethods(Flags);

            foreach (var method in methodInfos)
            {
                if (!method.IsDefined(typeof(MessageHandlerAttribute), true)) continue;
                if (method.Name != m_InvokeMethodName) continue;

                if (method.GetParameters().Length != 1)
                    throw new TargetParameterCountException("参数个数必须为1");

                if (method.GetParameters()[0].ParameterType.FullName != MessageType.FullName)
                    throw new ArgumentException("参数类型数必须为:" + MessageType.FullName);

                m_InvokeMethod = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 创建强类型消息处理委托。
        /// 说明（项目铁律 4）：消息类型 -&gt; 强类型委托的构造已由生成物
        /// <c>Generated/ProtoMessageRegistry.g.cs</c> 在编译期静态分派
        /// （<see cref="MessageDelegateFactory.Create"/>），不再使用
        /// <c>MethodInfo.MakeGenericMethod</c> / <c>GetMethod</c> 运行时反射。
        /// 仅在注册阶段执行一次；收包阶段直接调用缓存委托，不再有每包 MethodInfo.Invoke 的反射开销。
        /// 消息类型未出现在生成表中（proto 变更后未重新生成）或无法生成强类型委托的环境
        /// （如部分 AOT 配置）会退化为反射调用，仅保证功能可用。
        /// </summary>
        /// <param name="method">注册阶段匹配到的用户方法</param>
        /// <param name="messageHandler">消息处理对象实例</param>
        private Action<IMessageHandler, MessageObject> CreateInvokeDelegate(MethodInfo method, IMessageHandler messageHandler)
        {
            try
            {
                var invokeDelegate = MessageDelegateFactory.Create(MessageType, method, messageHandler);
                if (invokeDelegate != null) return invokeDelegate;

                FuLogger.LogWarning($"消息类型未在生成的委托工厂中，退化为反射调用：{MessageType.FullName}.{method.Name}");
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"创建消息处理委托失败，退化为反射调用：{method.Name} {e.Message}");
            }

            return (_, message) => method.Invoke(method.IsStatic ? null : messageHandler, new object[] { message });
        }
    }
}
