using System;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Abstractions.Events;

namespace FuFramework.Core.Utility;

/// <summary>
/// 代理调用回调
/// </summary>
/// <typeparam name="TAgent"></typeparam>
public abstract class AgentCallback<TAgent> : IAgentCallback where TAgent : IComponentAgent
{
	/// <summary>
	/// </summary>
	/// <returns></returns>
	public Type CompAgentType()
	{
		return typeof(TAgent);
	}

	/// <summary>
	/// 执行
	/// </summary>
	/// <param name="agent"></param>
	/// <param name="gameEventArgs"></param>
	/// <returns></returns>
	public Task<bool> Invoke(IComponentAgent agent, GameEventArgs gameEventArgs = null)
	{
		return OnCall((TAgent)agent, gameEventArgs);
	}

	/// <summary>
	/// 回调
	/// </summary>
	/// <param name="comp"></param>
	/// <param name="gameEventArgs"></param>
	/// <returns></returns>
	protected abstract Task<bool> OnCall(TAgent comp, GameEventArgs gameEventArgs);
}
