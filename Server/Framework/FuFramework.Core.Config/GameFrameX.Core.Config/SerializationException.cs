using System;

namespace FuFramework.Core.Config;

/// <summary>
/// </summary>
public class SerializationException : Exception
{
	/// <summary>
	/// </summary>
	public SerializationException()
	{
	}

	/// <summary>
	/// </summary>
	/// <param name="msg"></param>
	public SerializationException(string msg)
		: base(msg)
	{
	}

	/// <summary>
	/// </summary>
	/// <param name="message"></param>
	/// <param name="innerException"></param>
	public SerializationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
