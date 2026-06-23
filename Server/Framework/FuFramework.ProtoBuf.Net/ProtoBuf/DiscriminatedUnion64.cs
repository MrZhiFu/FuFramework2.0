using System;
using System.Runtime.InteropServices;

namespace ProtoBuf;

/// <summary>
/// Represent multiple types as a union; this is used as part of OneOf -
/// note that it is the caller's responsbility to only read/write the value as the same type
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct DiscriminatedUnion64
{
	/// <summary>The value typed as Int64</summary>
	[FieldOffset(8)]
	public readonly long Int64;

	/// <summary>The value typed as UInt64</summary>
	[FieldOffset(8)]
	public readonly ulong UInt64;

	/// <summary>The value typed as Int32</summary>
	[FieldOffset(8)]
	public readonly int Int32;

	/// <summary>The value typed as UInt32</summary>
	[FieldOffset(8)]
	public readonly uint UInt32;

	/// <summary>The value typed as Boolean</summary>
	[FieldOffset(8)]
	public readonly bool Boolean;

	/// <summary>The value typed as Single</summary>
	[FieldOffset(8)]
	public readonly float Single;

	/// <summary>The value typed as Double</summary>
	[FieldOffset(8)]
	public readonly double Double;

	/// <summary>The value typed as DateTime</summary>
	[FieldOffset(8)]
	public readonly DateTime DateTime;

	/// <summary>The value typed as TimeSpan</summary>
	[FieldOffset(8)]
	public readonly TimeSpan TimeSpan;

	/// <summary>The discriminator value</summary>
	[field: FieldOffset(0)]
	public int Discriminator { get; }

	unsafe static DiscriminatedUnion64()
	{
		if (sizeof(DateTime) > 8)
		{
			throw new InvalidOperationException("DateTime was unexpectedly too big for DiscriminatedUnion64");
		}
		if (sizeof(TimeSpan) > 8)
		{
			throw new InvalidOperationException("TimeSpan was unexpectedly too big for DiscriminatedUnion64");
		}
	}

	private DiscriminatedUnion64(int discriminator)
	{
		this = default(DiscriminatedUnion64);
		Discriminator = discriminator;
	}

	/// <summary>Indicates whether the specified discriminator is assigned</summary>
	public bool Is(int discriminator)
	{
		return Discriminator == discriminator;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, long value)
		: this(discriminator)
	{
		Int64 = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, int value)
		: this(discriminator)
	{
		Int32 = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, ulong value)
		: this(discriminator)
	{
		UInt64 = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, uint value)
		: this(discriminator)
	{
		UInt32 = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, float value)
		: this(discriminator)
	{
		Single = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, double value)
		: this(discriminator)
	{
		Double = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, bool value)
		: this(discriminator)
	{
		Boolean = value;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, DateTime? value)
		: this(value.HasValue ? discriminator : 0)
	{
		DateTime = value.GetValueOrDefault();
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnion64(int discriminator, TimeSpan? value)
		: this(value.HasValue ? discriminator : 0)
	{
		TimeSpan = value.GetValueOrDefault();
	}

	/// <summary>Reset a value if the specified discriminator is assigned</summary>
	public static void Reset(ref DiscriminatedUnion64 value, int discriminator)
	{
		if (value.Discriminator == discriminator)
		{
			value = default(DiscriminatedUnion64);
		}
	}
}
