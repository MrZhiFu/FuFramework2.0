using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Primitives;

/// <summary>
/// Provides access to an <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
/// </summary>
public interface ILoggerAccessor
{
	/// <summary>
	/// Gets the <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
	/// </summary>
	ILogger Logger { get; }
}
