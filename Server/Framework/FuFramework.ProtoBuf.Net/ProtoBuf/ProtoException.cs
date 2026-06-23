using System;

namespace ProtoBuf;

/// <summary>
/// Indicates an error during serialization/deserialization of a proto stream.
/// </summary>
public class ProtoException : Exception
{
	/// <summary>Creates a new ProtoException instance.</summary>
	public ProtoException()
	{
	}

	/// <summary>Creates a new ProtoException instance.</summary>
	public ProtoException(string message)
		: base(message)
	{
	}

	/// <summary>Creates a new ProtoException instance.</summary>
	public ProtoException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
