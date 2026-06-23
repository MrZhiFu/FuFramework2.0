using System;
using System.Runtime.InteropServices;

namespace ProtoBuf;

/// <summary>
/// Represents the outcome of computing the length of an object; since this may have required computing lengths
/// for multiple objects, some metadata is retained so that a subsequent serialize operation using
/// this instance can re-use the previously calculated lengths. If the object state changes between the
/// measure and serialize operations, the behavior is undefined.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct MeasureState<T> : IDisposable
{
	/// <summary>
	/// Gets the calculated length of this serialize operation, in bytes
	/// </summary>
	public long Length
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Releases all resources associated with this value
	/// </summary>
	public void Dispose()
	{
		throw new NotImplementedException();
	}
}
