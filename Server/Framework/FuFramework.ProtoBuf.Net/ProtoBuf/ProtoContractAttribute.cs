using System;

namespace ProtoBuf;

/// <summary>
/// Indicates that a type is defined for protocol-buffer serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
public sealed class ProtoContractAttribute : Attribute
{
	private const ushort OPTIONS_InferTagFromName = 1;

	private const ushort OPTIONS_InferTagFromNameHasValue = 2;

	private const ushort OPTIONS_UseProtoMembersOnly = 4;

	private const ushort OPTIONS_SkipConstructor = 8;

	private const ushort OPTIONS_IgnoreListHandling = 16;

	private const ushort OPTIONS_AsReferenceDefault = 32;

	private const ushort OPTIONS_EnumPassthru = 64;

	private const ushort OPTIONS_EnumPassthruHasValue = 128;

	private const ushort OPTIONS_IsGroup = 256;

	private ushort flags;

	private int implicitFirstTag;

	/// <summary>
	/// Gets or sets the defined name of the type.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the fist offset to use with implicit field tags;
	/// only uesd if ImplicitFields is set.
	/// </summary>
	public int ImplicitFirstTag
	{
		get
		{
			return implicitFirstTag;
		}
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException("ImplicitFirstTag");
			}
			implicitFirstTag = value;
		}
	}

	/// <summary>
	/// If specified, alternative contract markers (such as markers for XmlSerailizer or DataContractSerializer) are ignored.
	/// </summary>
	public bool UseProtoMembersOnly
	{
		get
		{
			return HasFlag(4);
		}
		set
		{
			SetFlag(4, value);
		}
	}

	/// <summary>
	/// If specified, do NOT treat this type as a list, even if it looks like one.
	/// </summary>
	public bool IgnoreListHandling
	{
		get
		{
			return HasFlag(16);
		}
		set
		{
			SetFlag(16, value);
		}
	}

	/// <summary>
	/// Gets or sets the mechanism used to automatically infer field tags
	/// for members. This option should be used in advanced scenarios only.
	/// Please review the important notes against the ImplicitFields enumeration.
	/// </summary>
	public ImplicitFields ImplicitFields { get; set; }

	/// <summary>
	/// Enables/disables automatic tag generation based on the existing name / order
	/// of the defined members. This option is not used for members marked
	/// with ProtoMemberAttribute, as intended to provide compatibility with
	/// WCF serialization. WARNING: when adding new fields you must take
	/// care to increase the Order for new elements, otherwise data corruption
	/// may occur.
	/// </summary>
	/// <remarks>If not explicitly specified, the default is assumed from Serializer.GlobalOptions.InferTagFromName.</remarks>
	public bool InferTagFromName
	{
		get
		{
			return HasFlag(1);
		}
		set
		{
			SetFlag(1, value);
			SetFlag(2, value: true);
		}
	}

	/// <summary>
	/// Has a InferTagFromName value been explicitly set? if not, the default from the type-model is assumed.
	/// </summary>
	internal bool InferTagFromNameHasValue => HasFlag(2);

	/// <summary>
	/// Specifies an offset to apply to [DataMember(Order=...)] markers;
	/// this is useful when working with mex-generated classes that have
	/// a different origin (usually 1 vs 0) than the original data-contract.
	/// This value is added to the Order of each member.
	/// </summary>
	public int DataMemberOffset { get; set; }

	/// <summary>
	/// If true, the constructor for the type is bypassed during deserialization, meaning any field initializers
	/// or other initialization code is skipped.
	/// </summary>
	public bool SkipConstructor
	{
		get
		{
			return HasFlag(8);
		}
		set
		{
			SetFlag(8, value);
		}
	}

	/// <summary>
	/// Should this type be treated as a reference by default? Please also see the implications of this,
	/// as recorded on ProtoMemberAttribute.AsReference
	/// </summary>
	public bool AsReferenceDefault
	{
		get
		{
			return HasFlag(32);
		}
		set
		{
			SetFlag(32, value);
		}
	}

	/// <summary>
	/// Indicates whether this type should always be treated as a "group" (rather than a string-prefixed sub-message)
	/// </summary>
	public bool IsGroup
	{
		get
		{
			return HasFlag(256);
		}
		set
		{
			SetFlag(256, value);
		}
	}

	/// <summary>
	/// Applies only to enums (not to DTO classes themselves); gets or sets a value indicating that an enum should be treated directly as an int/short/etc, rather
	/// than enforcing .proto enum rules. This is useful *in particul* for [Flags] enums.
	/// </summary>
	public bool EnumPassthru
	{
		get
		{
			return HasFlag(64);
		}
		set
		{
			SetFlag(64, value);
			SetFlag(128, value: true);
		}
	}

	/// <summary>
	/// Allows to define a surrogate type used for serialization/deserialization purpose.
	/// </summary>
	public Type Surrogate { get; set; }

	/// <summary>
	/// Has a EnumPassthru value been explicitly set?
	/// </summary>
	internal bool EnumPassthruHasValue => HasFlag(128);

	private bool HasFlag(ushort flag)
	{
		return (flags & flag) == flag;
	}

	private void SetFlag(ushort flag, bool value)
	{
		if (value)
		{
			flags |= flag;
		}
		else
		{
			flags = (ushort)(flags & ~flag);
		}
	}
}
