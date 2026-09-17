using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Primitives;

/// <summary>
/// Provides utility extension methods for handling tasks.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Ignores the result of the specified task.
	/// </summary>
	/// <param name="task">The task to ignore.</param>
	public static void DoNotAwait(this Task task)
	{
	}

	/// <summary>
	/// Ignores the result of the specified value task.
	/// </summary>
	/// <param name="task">The value task to ignore.</param>
	public static void DoNotAwait(this ValueTask task)
	{
	}
}
