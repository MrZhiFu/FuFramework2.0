using System;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Abstractions.Events;

namespace FuFramework.Core.Utility;

/// <summary>
/// 代理调用回调
/// </summary>
public interface IAgentCallback
{
	/// <summary>
	/// 执行
	/// </summary>
	/// <param name="agent">组件代理</param>
	/// <param name="gameEventArgs">参数</param>
	/// <returns></returns>
	Task<bool> Invoke(IComponentAgent agent, GameEventArgs gameEventArgs = null);

	/// <summary>
	/// 组件代理类型
	/// </summary>
	/// <returns></returns>
	Type CompAgentType();
}
