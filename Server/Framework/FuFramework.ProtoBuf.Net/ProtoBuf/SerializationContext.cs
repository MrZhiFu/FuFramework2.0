using System;

namespace ProtoBuf;

/// <summary>
/// Additional information about a serialization operation
/// </summary>
public sealed class SerializationContext
{
	private bool frozen;

	private object context;

	/// <summary>
	/// Gets or sets a user-defined object containing additional information about this serialization/deserialization operation.
	/// </summary>
	public object Context
	{
		get
		{
			return context;
		}
		set
		{
			if (context != value)
			{
				ThrowIfFrozen();
				context = value;
			}
		}
	}

	/// <summary>
	/// A default SerializationContext, with minimal information.
	/// </summary>
	internal static SerializationContext Default { get; }

	internal void Freeze()
	{
		frozen = true;
	}

	private void ThrowIfFrozen()
	{
		if (frozen)
		{
			throw new InvalidOperationException("The serialization-context cannot be changed once it is in use");
		}
	}

	static SerializationContext()
	{
		Default = new SerializationContext();
		Default.Freeze();
	}
}
