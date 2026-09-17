using System;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Components;
using FuFramework.Core.Utility;

namespace FuFramework.Core.BaseHandler;

/// <summary>
/// 全局组件处理器
/// </summary>
public abstract class GlobalComponentHandler : BaseComponentHandler
{
	/// <summary>
	/// 初始化
	/// </summary>
	/// <returns></returns>
	protected override Task InitActor()
	{
		if (base.ActorId <= 0)
		{
			ushort actorType = ComponentRegister.GetActorType(ComponentAgentType.BaseType.GetGenericArguments()[0]);
			base.ActorId = ActorIdGenerator.GetActorId(actorType);
		}
		return Task.CompletedTask;
	}
}
/// <summary>
/// 全局组件处理器
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class GlobalComponentHandler<T> : GlobalComponentHandler where T : IComponentAgent
{
	/// <summary>
	/// 组件代理类型
	/// </summary>
	protected override Type ComponentAgentType => typeof(T);

	/// <summary>
	/// 缓存组件代理对象
	/// </summary>
	protected T ComponentAgent => (T)base.CacheComponent;
}
