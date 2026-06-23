namespace ProtoBuf;

/// <summary>
/// Represent multiple types as a union; this is used as part of OneOf -
/// note that it is the caller's responsbility to only read/write the value as the same type
/// </summary>
public readonly struct DiscriminatedUnionObject
{
	/// <summary>The value typed as Object</summary>
	public readonly object Object;

	/// <summary>The discriminator value</summary>
	public int Discriminator { get; }

	/// <summary>Indicates whether the specified discriminator is assigned</summary>
	public bool Is(int discriminator)
	{
		return Discriminator == discriminator;
	}

	/// <summary>Create a new discriminated union value</summary>
	public DiscriminatedUnionObject(int discriminator, object value)
	{
		Discriminator = discriminator;
		Object = value;
	}

	/// <summary>Reset a value if the specified discriminator is assigned</summary>
	public static void Reset(ref DiscriminatedUnionObject value, int discriminator)
	{
		if (value.Discriminator == discriminator)
		{
			value = default(DiscriminatedUnionObject);
		}
	}
}
