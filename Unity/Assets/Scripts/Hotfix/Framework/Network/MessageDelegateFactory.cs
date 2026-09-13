using System;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 消息处理委托工厂（手写部分）。
    /// 功能：
    ///     1. 为「用户 [MessageHandler] 方法」构造强类型委托（Action&lt;TMessage&gt;），
    ///        收包阶段直接调用委托，不再有每包 MethodInfo.Invoke 的反射开销。
    ///
    /// 说明（项目铁律 4）：消息类型 -&gt; CreateTyped&lt;T&gt; 的静态分派由生成部分提供
    /// （<c>Generated/ProtoMessageRegistry.g.cs</c>），取代原先的
    /// <c>MethodInfo.MakeGenericMethod</c> + <c>GetMethod</c> 运行时反射调用。
    ///
    /// 关于残留反射：查找用户方法本身仍需 <c>Type.GetMethods</c>（用户方法可能是
    /// 游戏侧类型的私有方法，生成期无法为其构造委托），
    /// 但委托的泛型构造已完全静态化。
    /// </summary>
    internal static partial class MessageDelegateFactory
    {
        /// <summary>
        /// 构造强类型委托：TMessage 由生成部分在编译期静态确定。
        /// </summary>
        /// <param name="method">注册阶段匹配到的用户方法</param>
        /// <param name="messageHandler">消息处理对象实例（静态方法时忽略）</param>
        private static Action<IMessageHandler, MessageObject> CreateTyped<TMessage>(MethodInfo method, IMessageHandler messageHandler)
            where TMessage : MessageObject
        {
            if (method.IsStatic)
            {
                var staticDelegate = (Action<TMessage>)method.CreateDelegate(typeof(Action<TMessage>));
                return (_, message) => staticDelegate((TMessage)message);
            }

            var instanceDelegate = (Action<TMessage>)method.CreateDelegate(typeof(Action<TMessage>), messageHandler);
            return (_, message) => instanceDelegate((TMessage)message);
        }
    }
}
