using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using AOT.Framework.Extension;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 网络非RPC返回消息处理器属性定义（**声明用**）。
    ///
    /// 说明（项目铁律 4：运行时杜绝反射）：
    ///     本特性只用于在源码里「声明」某个方法要处理某个消息类型，供生成脚本
    ///     <c>Protobuf/gen-proto-registry.py</c> 扫描。运行期的注册与派发完全不再读取特性、不再查找方法：
    ///     生成物 <c>Generated/ProtoMessageRegistry.g.cs</c> 会为每个 [MessageHandler] 方法直接产出
    ///     强类型委托，由 <see cref="ProtoMessageHandler.Add"/> 绑定到本类的实例上。
    ///     因此运行时不存在 <c>GetMethods</c> / <c>IsDefined</c> / <c>GetCustomAttribute</c> /
    ///     <c>CreateDelegate</c> / <c>MethodInfo.Invoke</c>。
    ///
    /// 可见性契约：
    ///     目标方法必须为 <c>internal</c> 或 <c>public</c>（生成物位于同程序集
    ///     <c>Hotfix.Framework.Network</c>，需能直接调用）。若是 <c>private</c> / <c>protected</c>，
    ///     生成脚本会**报错并中止**，提示改为 internal/public。
    /// </summary>
    /// <example>
    /// <code>
    /// public sealed class BagManager : Singleton&lt;BagManager&gt;, IMessageHandler
    /// {
    ///     [MessageHandler(typeof(NotifyBagInfoChanged), nameof(NotifyBagInfoChanged))]
    ///     internal void NotifyBagInfoChanged(NotifyBagInfoChanged msg) { /* ... */ }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// 单个方法允许积压的最大未处理消息数量，防止消息队列无界增长。
        /// </summary>
        private const int MaxQueuedMessageCount = 256;

        /// <summary>
        /// 消息对象
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// 执行的方法名称（仅用于日志与去重比对，不参与任何反射查找）
        /// </summary>
        private readonly string m_InvokeMethodName;

        /// <summary>
        /// 消息处理器
        /// </summary>
        private IMessageHandler m_MessageHandler;

        /// <summary>
        /// 已绑定的消息处理委托。
        /// 由生成物直接产出（形如 <c>static (handler, message) =&gt; ((BagManager)handler).OnX((X)message)</c>），
        /// 注册阶段一次性绑定，收包阶段直接调用，全程无反射。
        /// </summary>
        private Action<IMessageHandler, MessageObject> m_InvokeDelegate;

        /// <summary>
        /// 消息处理对象队列
        /// </summary>
        private readonly Queue<MessageObject> m_MessageObjects = new();

        /// <summary>
        /// 网络消息处理器
        /// </summary>
        /// <param name="message">注册的消息对象。需要继承MessageObject和实现INotifyMessage</param>
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
        /// 注册时的方法名（仅用于日志与去重比对）
        /// </summary>
        internal string InvokeMethodName => m_InvokeMethodName;

        /// <summary>
        /// 注册的处理对象实例（用于显式比对，避免依赖 Attribute 的引用相等性）
        /// </summary>
        internal IMessageHandler TargetHandler => m_MessageHandler;

        /// <summary>
        /// 绑定处理对象与生成物产出的强类型委托。
        /// 由 <see cref="ProtoMessageHandler.Add"/> 调用；不再做任何方法查找。
        /// </summary>
        /// <param name="messageHandler">消息处理对象实例</param>
        /// <param name="invokeDelegate">生成物产出的直接委托</param>
        internal void Bind(IMessageHandler messageHandler, Action<IMessageHandler, MessageObject> invokeDelegate)
        {
            MessageType.NotNull(nameof(MessageType));
            messageHandler.NotNull(nameof(messageHandler));
            invokeDelegate.NotNull(nameof(invokeDelegate));
            m_MessageHandler = messageHandler;
            m_InvokeDelegate = invokeDelegate;
        }

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
                throw new ArgumentNullException(nameof(m_InvokeDelegate), $"未绑定处理委托：{m_InvokeMethodName}.请确认是否注册成功");

            m_InvokeDelegate(m_MessageHandler, messageObject);
        }
    }
}
