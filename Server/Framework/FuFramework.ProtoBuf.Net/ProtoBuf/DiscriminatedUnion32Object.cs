using System.Runtime.InteropServices;

namespace ProtoBuf;

/// <summary>
/// Represent multiple types as a union; this is used as part of OneOf -
/// note that it is the caller's responsbility to only read/write the value as the same type
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct DiscriminatedUnion32Object
{
	/// <summary>The value typed as Int32</summary>
	[FieldOffset(4)]
	public readonly int Int32;

	/// <summary>The value typed as UInt32</summary>
	[FieldOffset(4)]
	public readonly uint UInt32;

	/// <summary>The value typed as Boolean</summary>
	[FieldOffset(4)]
	public readonly bool Boolean;

	/// <summary>The value typed as Single</summary>
	[FieldOffset(4)]
	public readonly float Single;

	/// <summary>The value typed as Object</summary>
	[FieldOffset(8)]
	public readonly object Object;

	/// <summary>The discriminator value</summary>
	[field: FieldOffset(0)]
	public int Discriminator { get; }

	private DiscriminatedUnion32Object(int discriminator)
	{
		this = default(DiscriminatedUnion32Object);
		Discriminator = discriminator;
	}

	/// <summary>Indicates whether the specified discriminator is assigned</summary>
	public bool Is(int discriminator)
	{
		return Discriminator == discriminator;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion32Object(int discriminator, int value)
		: this(discriminator)
	{
		Int32 = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion32Object(int discriminator, uint value)
		: this(discriminator)
	{
		UInt32 = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion32Object(int discriminator, float value)
		: this(discriminator)
	{
		Single = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion32Object(int discriminator, bool value)
		: this(discriminator)
	{
		Boolean = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion32Object(int discriminator, object value)
		: this((value != null) ? discriminator : 0)
	{
		Object = value;
	}

	/// <summary>Reset a value if the specified discriminator is assigned</summary>
	public static void Reset(ref DiscriminatedUnion32Object value, int discriminator)
	{
		if (value.Discriminator == discriminator)
		{
			value = default(DiscriminatedUnion32Object);
		}
	}
}
