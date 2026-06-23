using System;

namespace ProtoBuf.Meta;

/// <summary>
/// Contains the stack-trace of the owning code when a lock-contention scenario is detected
/// </summary>
public sealed class LockContentedEventArgs : EventArgs
{
	/// <summary>
	/// The stack-trace of the code that owned the lock when a lock-contention scenario occurred
	/// </summary>
	public string OwnerStackTrace { get; }

	internal LockContentedEventArgs(string ownerStackTrace)
	{
		OwnerStackTrace = ownerStackTrace;
	}
}
