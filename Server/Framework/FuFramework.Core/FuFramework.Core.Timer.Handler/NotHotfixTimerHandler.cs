using System.Threading.Tasks;
using FuFramework.Core.Abstractions.Events;
using Quartz;

namespace FuFramework.Core.Timer.Handler;

/// <summary>
/// 非热更程序集的计时器处理器，不需要热更时间更新
/// </summary>
public abstract class NotHotfixTimerHandler : IJob
{
	/// <summary>
	/// 内部计时器处理器调用函数
	/// </summary>
	/// <param name="context">Quartz 作业执行上下文，包含作业执行所需的信息</param>
	/// <returns>一个任务，表示异步操作的结果</returns>
	public Task Execute(IJobExecutionContext context)
	{
		GameEventArgs gameEventArgs = context.JobDetail.JobDataMap.Get("param") as GameEventArgs;
		return HandleTimer(gameEventArgs);
	}

	/// <summary>
	/// 计时器处理函数
	/// </summary>
	/// <param name="gameEventArgs">传递给处理器的参数</param>
	/// <returns>一个任务，表示异步操作的结果</returns>
	protected abstract Task HandleTimer(GameEventArgs gameEventArgs);
}
