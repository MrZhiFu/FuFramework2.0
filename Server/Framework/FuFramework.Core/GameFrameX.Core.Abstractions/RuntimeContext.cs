using System.Runtime.CompilerServices;
using System.Threading;

namespace FuFramework.Core.Abstractions;

/// <summary>
/// 运行时上下文
/// </summary>
public static class RuntimeContext
{
	/// <summary>
	/// 当前链上下文
	/// </summary>
	private static readonly AsyncLocal<long> ChainContext = new AsyncLocal<long>();

	/// <summary>
	/// 当前Actor上下文
	/// </summary>
	private static readonly AsyncLocal<long> ActorContext = new AsyncLocal<long>();

	/// <summary>
	/// 当前链ID
	/// </summary>
	public static long CurrentChainId => ChainContext.Value;

	/// <summary>
	/// 当前ActorID
	/// </summary>
	public static long CurrentActor => ActorContext.Value;

	/// <summary>
	/// 设置上下文
	/// </summary>
	/// <param name="callChainId"></param>
	/// <param name="actorId"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetContext(long callChainId, long actorId)
	{
		ChainContext.Value = callChainId;
		ActorContext.Value = actorId;
	}

	/// <summary>
	/// 重置上下文
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ResetContext()
	{
		ChainContext.Value = 0L;
		ActorContext.Value = 0L;
	}
}
