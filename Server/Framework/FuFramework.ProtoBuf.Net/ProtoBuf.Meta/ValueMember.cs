using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta;

/// <summary>
/// Represents a member (property/field) that is mapped to a protobuf field
/// </summary>
public class ValueMember
{
	internal sealed class Comparer : IComparer, IComparer<ValueMember>
	{
		public static readonly Comparer Default = new Comparer();

		public int Compare(object x, object y)
		{
			return Compare(x as ValueMember, y as ValueMember);
		}

		public int Compare(ValueMember x, ValueMember y)
		{
			if (x == y)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			return x.FieldNumber.CompareTo(y.FieldNumber);
		}
	}

	private const byte OPTIONS_IsStrict = 1;

	private const byte OPTIONS_IsPacked = 2;

	private const byte OPTIONS_IsRequired = 4;

	private const byte OPTIONS_OverwriteList = 8;

	private const byte OPTIONS_SupportNull = 16;

	private const byte OPTIONS_AsReference = 32;

	private const byte OPTIONS_IsMap = 64;

	private const byte OPTIONS_DynamicType = 128;

	private readonly RuntimeTypeModel model;

	private int _fieldNumber;

	private MemberInfo backingMember;

	private DataFormat dataFormat;

	private object defaultValue;

	private byte flags;

	private MethodInfo getSpecified;

	private MethodInfo setSpecified;

	private DataFormat mapKeyFormat;

	private DataFormat mapValueFormat;

	private string name;

	private IProtoSerializer serializer;

	/// <summary>
	/// The number that identifies this member in a protobuf stream
	/// </summary>
	public int FieldNumber
	{
		get
		{
			return _fieldNumber;
		}
		internal set
		{
			if (_fieldNumber != value)
			{
				MetaType.AssertValidFieldNumber(value);
				ThrowIfFrozen();
				_fieldNumber = value;
			}
		}
	}

	/// <summary>
	/// Gets the member (field/property) which this member relates to.
	/// </summary>
	public MemberInfo Member { get; }

	/// <summary>
	/// Gets the backing member (field/property) which this member relates to
	/// </summary>
	public MemberInfo BackingMember
	{
		get
		{
			return backingMember;
		}
		set
		{
			if (backingMember != value)
			{
				ThrowIfFrozen();
				backingMember = value;
			}
		}
	}

	/// <summary>
	/// Within a list / array / etc, the type of object for each item in the list (especially useful with ArrayList)
	/// </summary>
	public Type ItemType { get; }

	/// <summary>
	/// The underlying type of the member
	/// </summary>
	public Type MemberType { get; }

	/// <summary>
	/// For abstract types (IList etc), the type of concrete object to create (if required)
	/// </summary>
	public Type DefaultType { get; }

	/// <summary>
	/// The type the defines the member
	/// </summary>
	public Type ParentType { get; }

	/// <summary>
	/// The default value of the item (members with this value will not be serialized)
	/// </summary>
	public object DefaultValue
	{
		get
		{
			return defaultValue;
		}
		set
		{
			if (defaultValue != value)
			{
				ThrowIfFrozen();
				defaultValue = value;
			}
		}
	}

	internal IProtoSerializer Serializer => serializer ?? (serializer = BuildSerializer());

	/// <summary>
	/// Specifies the rules used to process the field; this is used to determine the most appropriate
	/// wite-type, but also to describe subtypes <i>within</i> that wire-type (such as SignedVariant)
	/// </summary>
	public DataFormat DataFormat
	{
		get
		{
			return dataFormat;
		}
		set
		{
			if (value != dataFormat)
			{
				ThrowIfFrozen();
				dataFormat = value;
			}
		}
	}

	/// <summary>
	/// Indicates whether this field should follow strict encoding rules; this means (for example) that if a "fixed32"
	/// is encountered when "variant" is defined, then it will fail (throw an exception) when parsing. Note that
	/// when serializing the defined type is always used.
	/// </summary>
	public bool IsStrict
	{
		get
		{
			return HasFlag(1);
		}
		set
		{
			SetFlag(1, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Indicates whether this field should use packed encoding (which can save lots of space for repeated primitive values).
	/// This option only applies to list/array data of primitive types (int, double, etc).
	/// </summary>
	public bool IsPacked
	{
		get
		{
			return HasFlag(2);
		}
		set
		{
			SetFlag(2, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Indicates whether this field should *repace* existing values (the default is false, meaning *append*).
	/// This option only applies to list/array data.
	/// </summary>
	public bool OverwriteList
	{
		get
		{
			return HasFlag(8);
		}
		set
		{
			SetFlag(8, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Indicates whether this field is mandatory.
	/// </summary>
	public bool IsRequired
	{
		get
		{
			return HasFlag(4);
		}
		set
		{
			SetFlag(4, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Enables full object-tracking/full-graph support.
	/// </summary>
	public bool AsReference
	{
		get
		{
			return HasFlag(32);
		}
		set
		{
			SetFlag(32, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Embeds the type information into the stream, allowing usage with types not known in advance.
	/// </summary>
	public bool DynamicType
	{
		get
		{
			return HasFlag(128);
		}
		set
		{
			SetFlag(128, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Indicates that the member should be treated as a protobuf Map
	/// </summary>
	public bool IsMap
	{
		get
		{
			return HasFlag(64);
		}
		set
		{
			SetFlag(64, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Specifies the data-format that should be used for the key, when IsMap is enabled
	/// </summary>
	public DataFormat MapKeyFormat
	{
		get
		{
			return mapKeyFormat;
		}
		set
		{
			if (mapKeyFormat != value)
			{
				ThrowIfFrozen();
				mapKeyFormat = value;
			}
		}
	}

	/// <summary>
	/// Specifies the data-format that should be used for the value, when IsMap is enabled
	/// </summary>
	public DataFormat MapValueFormat
	{
		get
		{
			return mapValueFormat;
		}
		set
		{
			if (mapValueFormat != value)
			{
				ThrowIfFrozen();
				mapValueFormat = value;
			}
		}
	}

	/// <summary>
	/// Gets the logical name for this member in the schema (this is not critical for binary serialization, but may be used
	/// when inferring a schema).
	/// </summary>
	public string Name
	{
		get
		{
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			return Member.Name;
		}
		set
		{
			SetName(value);
		}
	}

	/// <summary>
	/// Should lists have extended support for null values? Note this makes the serialization less efficient.
	/// </summary>
	public bool SupportNull
	{
		get
		{
			return HasFlag(16);
		}
		set
		{
			SetFlag(16, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Creates a new ValueMember instance
	/// </summary>
	public ValueMember(RuntimeTypeModel model, Type parentType, int fieldNumber, MemberInfo member, Type memberType, Type itemType, Type defaultType, DataFormat dataFormat, object defaultValue)
		: this(model, fieldNumber, memberType, itemType, defaultType, dataFormat)
	{
		if (parentType == null)
		{
			throw new ArgumentNullException("parentType");
		}
		if (fieldNumber < 1 && !Helpers.IsEnum(parentType))
		{
			throw new ArgumentOutOfRangeException("fieldNumber");
		}
		Member = member ?? throw new ArgumentNullException("member");
		ParentType = parentType;
		if (fieldNumber < 1 && !Helpers.IsEnum(parentType))
		{
			throw new ArgumentOutOfRangeException("fieldNumber");
		}
		if (defaultValue != null && model.MapType(defaultValue.GetType()) != memberType)
		{
			defaultValue = ParseDefaultValue(memberType, defaultValue);
		}
		this.defaultValue = defaultValue;
		MetaType metaType = model.FindWithoutAdd(memberType);
		if (metaType != null)
		{
			AsReference = metaType.AsReferenceDefault;
		}
		else
		{
			AsReference = MetaType.GetAsReferenceDefault(model, memberType);
		}
	}

	/// <summary>
	/// Creates a new ValueMember instance
	/// </summary>
	internal ValueMember(RuntimeTypeModel model, int fieldNumber, Type memberType, Type itemType, Type defaultType, DataFormat dataFormat)
	{
		_fieldNumber = fieldNumber;
		MemberType = memberType ?? throw new ArgumentNullException("memberType");
		ItemType = itemType;
		DefaultType = defaultType;
		this.model = model ?? throw new ArgumentNullException("model");
		this.dataFormat = dataFormat;
	}

	internal object GetRawEnumValue()
	{
		return ((FieldInfo)Member).GetRawConstantValue();
	}

	private static object ParseDefaultValue(Type type, object value)
	{
		Type underlyingType = Helpers.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			type = underlyingType;
		}
		if (value is string text)
		{
			if (Helpers.IsEnum(type))
			{
				return Helpers.ParseEnum(type, text);
			}
			switch (Helpers.GetTypeCode(type))
			{
			case ProtoTypeCode.Boolean:
				return bool.Parse(text);
			case ProtoTypeCode.Byte:
				return byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Char:
				if (text.Length == 1)
				{
					return text[0];
				}
				throw new FormatException("Single character expected: \"" + text + "\"");
			case ProtoTypeCode.DateTime:
				return DateTime.Parse(text, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Decimal:
				return decimal.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Double:
				return double.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Int16:
				return short.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Int32:
				return int.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Int64:
				return long.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.SByte:
				return sbyte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
			case ProtoTypeCode.Single:
				return float.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.String:
				return text;
			case ProtoTypeCode.UInt16:
				return ushort.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.UInt32:
				return uint.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.UInt64:
				return ulong.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);
			case ProtoTypeCode.TimeSpan:
				return TimeSpan.Parse(text);
			case ProtoTypeCode.Uri:
				return text;
			case ProtoTypeCode.Guid:
				return new Guid(text);
			}
		}
		if (Helpers.IsEnum(type))
		{
			return Enum.ToObject(type, value);
		}
		return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Specifies methods for working with optional data members.
	/// </summary>
	/// <param name="getSpecified">
	/// Provides a method (null for none) to query whether this member should
	/// be serialized; it must be of the form "bool {Method}()". The member is only serialized if the
	/// method returns true.
	/// </param>
	/// <param name="setSpecified">
	/// Provides a method (null for none) to indicate that a member was
	/// deserialized; it must be of the form "void {Method}(bool)", and will be called with "true"
	/// when data is found.
	/// </param>
	public void SetSpecified(MethodInfo getSpecified, MethodInfo setSpecified)
	{
		if (this.getSpecified != getSpecified || this.setSpecified != setSpecified)
		{
			if (getSpecified != null && (getSpecified.ReturnType != model.MapType(typeof(bool)) || getSpecified.IsStatic || getSpecified.GetParameters().Length != 0))
			{
				throw new ArgumentException("Invalid pattern for checking member-specified", "getSpecified");
			}
			ParameterInfo[] parameters;
			if (setSpecified != null && (setSpecified.ReturnType != model.MapType(typeof(void)) || setSpecified.IsStatic || (parameters = setSpecified.GetParameters()).Length != 1 || parameters[0].ParameterType != model.MapType(typeof(bool))))
			{
				throw new ArgumentException("Invalid pattern for setting member-specified", "setSpecified");
			}
			ThrowIfFrozen();
			this.getSpecified = getSpecified;
			this.setSpecified = setSpecified;
		}
	}

	private void ThrowIfFrozen()
	{
		if (serializer != null)
		{
			throw new InvalidOperationException("The type cannot be changed once a serializer has been generated");
		}
	}

	internal bool ResolveMapTypes(out Type dictionaryType, out Type keyType, out Type valueType)
	{
		dictionaryType = (keyType = (valueType = null));
		try
		{
			Type memberType = MemberType;
			if (ImmutableCollectionDecorator.IdentifyImmutable(model, MemberType, out MethodInfo _, out PropertyInfo _, out PropertyInfo _, out MethodInfo _, out MethodInfo _, out MethodInfo _))
			{
				return false;
			}
			if (memberType.IsInterface && memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(IDictionary<, >))
			{
				Type[] genericArguments = MemberType.GetGenericArguments();
				if (IsValidMapKeyType(genericArguments[0]))
				{
					keyType = genericArguments[0];
					valueType = genericArguments[1];
					dictionaryType = MemberType;
				}
				return false;
			}
			Type[] interfaces = MemberType.GetInterfaces();
			foreach (Type type in interfaces)
			{
				memberType = type;
				if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(IDictionary<, >))
				{
					if (dictionaryType != null)
					{
						throw new InvalidOperationException("Multiple dictionary interfaces implemented by type: " + MemberType.FullName);
					}
					Type[] genericArguments2 = type.GetGenericArguments();
					if (IsValidMapKeyType(genericArguments2[0]))
					{
						keyType = genericArguments2[0];
						valueType = genericArguments2[1];
						dictionaryType = MemberType;
					}
				}
			}
			if (dictionaryType == null)
			{
				return false;
			}
			Type itemType = null;
			Type defaultType = null;
			model.ResolveListTypes(valueType, ref itemType, ref defaultType);
			if (itemType != null)
			{
				return false;
			}
			return dictionaryType != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsValidMapKeyType(Type type)
	{
		if (type == null || Helpers.IsEnum(type))
		{
			return false;
		}
		ProtoTypeCode typeCode = Helpers.GetTypeCode(type);
		if ((uint)(typeCode - 3) <= 9u || typeCode == ProtoTypeCode.String)
		{
			return true;
		}
		return false;
	}

	private IProtoSerializer BuildSerializer()
	{
		int opaqueToken = 0;
		try
		{
			model.TakeLock(ref opaqueToken);
			MemberInfo memberInfo = backingMember ?? Member;
			IProtoSerializer protoSerializer3;
			if (IsMap)
			{
				ResolveMapTypes(out Type dictionaryType, out Type keyType, out Type valueType);
				if (dictionaryType == null)
				{
					throw new InvalidOperationException("Unable to resolve map type for type: " + MemberType.FullName);
				}
				Type type = DefaultType;
				if (type == null && Helpers.IsClass(MemberType))
				{
					type = MemberType;
				}
				WireType defaultWireType;
				IProtoSerializer protoSerializer = TryGetCoreSerializer(model, MapKeyFormat, keyType, out defaultWireType, asReference: false, dynamicType: false, overwriteList: false, allowComplexTypes: false);
				if (!AsReference)
				{
					AsReference = MetaType.GetAsReferenceDefault(model, valueType);
				}
				WireType defaultWireType2;
				IProtoSerializer protoSerializer2 = TryGetCoreSerializer(model, MapValueFormat, valueType, out defaultWireType2, AsReference, DynamicType, overwriteList: false, allowComplexTypes: true);
				ConstructorInfo[] constructors = typeof(MapDecorator<, , >).MakeGenericType(dictionaryType, keyType, valueType).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (constructors.Length != 1)
				{
					throw new InvalidOperationException("Unable to resolve MapDecorator constructor");
				}
				protoSerializer3 = (IProtoSerializer)constructors[0].Invoke(new object[9]
				{
					model,
					type,
					protoSerializer,
					protoSerializer2,
					_fieldNumber,
					(DataFormat == DataFormat.Group) ? WireType.StartGroup : WireType.String,
					defaultWireType,
					defaultWireType2,
					OverwriteList
				});
			}
			else
			{
				Type type2 = ItemType ?? MemberType;
				protoSerializer3 = TryGetCoreSerializer(model, dataFormat, type2, out var defaultWireType3, AsReference, DynamicType, OverwriteList, allowComplexTypes: true);
				if (protoSerializer3 == null)
				{
					throw new InvalidOperationException("No serializer defined for type: " + type2.FullName);
				}
				if (ItemType != null && SupportNull)
				{
					if (IsPacked)
					{
						throw new NotSupportedException("Packed encodings cannot support null values");
					}
					protoSerializer3 = new TagDecorator(1, defaultWireType3, IsStrict, protoSerializer3);
					protoSerializer3 = new NullDecorator(model, protoSerializer3);
					protoSerializer3 = new TagDecorator(_fieldNumber, WireType.StartGroup, strict: false, protoSerializer3);
				}
				else
				{
					protoSerializer3 = new TagDecorator(_fieldNumber, defaultWireType3, IsStrict, protoSerializer3);
				}
				if (ItemType != null)
				{
					if (!SupportNull)
					{
						if ((object)Helpers.GetUnderlyingType(ItemType) == null)
						{
							_ = ItemType;
						}
					}
					else
					{
						_ = ItemType;
					}
					protoSerializer3 = ((!MemberType.IsArray) ? ((ProtoDecoratorBase)ListDecorator.Create(model, MemberType, DefaultType, protoSerializer3, _fieldNumber, IsPacked, defaultWireType3, memberInfo != null && PropertyDecorator.CanWrite(model, memberInfo), OverwriteList, SupportNull)) : ((ProtoDecoratorBase)new ArrayDecorator(model, protoSerializer3, _fieldNumber, IsPacked, defaultWireType3, MemberType, OverwriteList, SupportNull)));
				}
				else if (defaultValue != null && !IsRequired && getSpecified == null)
				{
					protoSerializer3 = new DefaultValueDecorator(model, defaultValue, protoSerializer3);
				}
				if (MemberType == model.MapType(typeof(Uri)))
				{
					protoSerializer3 = new UriDecorator(model, protoSerializer3);
				}
			}
			if (memberInfo != null)
			{
				if (memberInfo is PropertyInfo property)
				{
					protoSerializer3 = new PropertyDecorator(model, ParentType, property, protoSerializer3);
				}
				else
				{
					if (!(memberInfo is FieldInfo field))
					{
						throw new InvalidOperationException();
					}
					protoSerializer3 = new FieldDecorator(ParentType, field, protoSerializer3);
				}
				if (getSpecified != null || setSpecified != null)
				{
					protoSerializer3 = new MemberSpecifiedDecorator(getSpecified, setSpecified, protoSerializer3);
				}
			}
			return protoSerializer3;
		}
		finally
		{
			model.ReleaseLock(opaqueToken);
		}
	}

	private static WireType GetIntWireType(DataFormat format, int width)
	{
		switch (format)
		{
		case DataFormat.ZigZag:
			return WireType.SignedVariant;
		case DataFormat.FixedSize:
			if (width != 32)
			{
				return WireType.Fixed64;
			}
			return WireType.Fixed32;
		case DataFormat.Default:
		case DataFormat.TwosComplement:
			return WireType.Variant;
		default:
			throw new InvalidOperationException();
		}
	}

	private static WireType GetDateTimeWireType(DataFormat format)
	{
		switch (format)
		{
		case DataFormat.Group:
			return WireType.StartGroup;
		case DataFormat.FixedSize:
			return WireType.Fixed64;
		case DataFormat.Default:
		case DataFormat.WellKnown:
			return WireType.String;
		default:
			throw new InvalidOperationException();
		}
	}

	internal static IProtoSerializer TryGetCoreSerializer(RuntimeTypeModel model, DataFormat dataFormat, Type type, out WireType defaultWireType, bool asReference, bool dynamicType, bool overwriteList, bool allowComplexTypes)
	{
		Type underlyingType = Helpers.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			type = underlyingType;
		}
		if (Helpers.IsEnum(type))
		{
			if (allowComplexTypes && model != null)
			{
				defaultWireType = WireType.Variant;
				return new EnumSerializer(type, model.GetEnumMap(type));
			}
			defaultWireType = WireType.None;
			return null;
		}
		switch (Helpers.GetTypeCode(type))
		{
		case ProtoTypeCode.Int32:
			defaultWireType = GetIntWireType(dataFormat, 32);
			return new Int32Serializer(model);
		case ProtoTypeCode.UInt32:
			defaultWireType = GetIntWireType(dataFormat, 32);
			return new UInt32Serializer(model);
		case ProtoTypeCode.Int64:
			defaultWireType = GetIntWireType(dataFormat, 64);
			return new Int64Serializer(model);
		case ProtoTypeCode.UInt64:
			defaultWireType = GetIntWireType(dataFormat, 64);
			return new UInt64Serializer(model);
		case ProtoTypeCode.String:
			defaultWireType = WireType.String;
			if (asReference)
			{
				return new NetObjectSerializer(model, model.MapType(typeof(string)), 0, BclHelpers.NetObjectOptions.AsReference);
			}
			return new StringSerializer(model);
		case ProtoTypeCode.Single:
			defaultWireType = WireType.Fixed32;
			return new SingleSerializer(model);
		case ProtoTypeCode.Double:
			defaultWireType = WireType.Fixed64;
			return new DoubleSerializer(model);
		case ProtoTypeCode.Boolean:
			defaultWireType = WireType.Variant;
			return new BooleanSerializer(model);
		case ProtoTypeCode.DateTime:
			defaultWireType = GetDateTimeWireType(dataFormat);
			return new DateTimeSerializer(dataFormat, model);
		case ProtoTypeCode.Decimal:
			defaultWireType = WireType.String;
			return new DecimalSerializer(model);
		case ProtoTypeCode.Byte:
			defaultWireType = GetIntWireType(dataFormat, 32);
			return new ByteSerializer(model);
		case ProtoTypeCode.SByte:
			defaultWireType = GetIntWireType(dataFormat, 32);
			return new SByteSerializer(model);
		case ProtoTypeCode.Char:
			defaultWireType = WireType.Variant;
			return new CharSerializer(model);
		case ProtoTypeCode.Int16:
			defaultWireType = GetIntWireType(dataFormat, 32);
			return new Int16Serializer(model);
		case ProtoTypeCode.UInt16:
			defaultWireType = GetIntWireType(dataFormat, 32);
			return new UInt16Serializer(model);
		case ProtoTypeCode.TimeSpan:
			defaultWireType = GetDateTimeWireType(dataFormat);
			return new TimeSpanSerializer(dataFormat, model);
		case ProtoTypeCode.Guid:
			defaultWireType = ((dataFormat == DataFormat.Group) ? WireType.StartGroup : WireType.String);
			return new GuidSerializer(model);
		case ProtoTypeCode.Uri:
			defaultWireType = WireType.String;
			return new StringSerializer(model);
		case ProtoTypeCode.ByteArray:
			defaultWireType = WireType.String;
			return new BlobSerializer(model, overwriteList);
		case ProtoTypeCode.Type:
			defaultWireType = WireType.String;
			return new SystemTypeSerializer(model);
		default:
		{
			IProtoSerializer protoSerializer = (model.AllowParseableTypes ? ParseableSerializer.TryCreate(type, model) : null);
			if (protoSerializer != null)
			{
				defaultWireType = WireType.String;
				return protoSerializer;
			}
			if (allowComplexTypes && model != null)
			{
				int key = model.GetKey(type, demand: false, getBaseKey: true);
				MetaType metaType = null;
				if (key >= 0)
				{
					metaType = model[type];
					if (dataFormat == DataFormat.Default && metaType.IsGroup)
					{
						dataFormat = DataFormat.Group;
					}
				}
				if (asReference || dynamicType)
				{
					BclHelpers.NetObjectOptions netObjectOptions = BclHelpers.NetObjectOptions.None;
					if (asReference)
					{
						netObjectOptions |= BclHelpers.NetObjectOptions.AsReference;
					}
					if (dynamicType)
					{
						netObjectOptions |= BclHelpers.NetObjectOptions.DynamicType;
					}
					if (metaType != null)
					{
						if (asReference && Helpers.IsValueType(type))
						{
							string text = "AsReference cannot be used with value-types";
							text = ((!(type.Name == "KeyValuePair`2")) ? (text + ": " + type.FullName) : (text + "; please see https://stackoverflow.com/q/14436606/23354"));
							throw new InvalidOperationException(text);
						}
						if (asReference && metaType.IsAutoTuple)
						{
							netObjectOptions |= BclHelpers.NetObjectOptions.LateSet;
						}
						if (metaType.UseConstructor)
						{
							netObjectOptions |= BclHelpers.NetObjectOptions.UseConstructor;
						}
					}
					defaultWireType = ((dataFormat == DataFormat.Group) ? WireType.StartGroup : WireType.String);
					return new NetObjectSerializer(model, type, key, netObjectOptions);
				}
				if (key >= 0)
				{
					defaultWireType = ((dataFormat == DataFormat.Group) ? WireType.StartGroup : WireType.String);
					return new SubItemSerializer(type, key, metaType, recursionCheck: true);
				}
			}
			defaultWireType = WireType.None;
			return null;
		}
		}
	}

	internal void SetName(string name)
	{
		if (name != this.name)
		{
			ThrowIfFrozen();
			this.name = name;
		}
	}

	private bool HasFlag(byte flag)
	{
		return (flags & flag) == flag;
	}

	private void SetFlag(byte flag, bool value, bool throwIfFrozen)
	{
		if (throwIfFrozen && HasFlag(flag) != value)
		{
			ThrowIfFrozen();
		}
		if (value)
		{
			flags |= flag;
		}
		else
		{
			flags = (byte)(flags & ~flag);
		}
	}

	internal string GetSchemaTypeName(bool applyNetObjectProxy, ref RuntimeTypeModel.CommonImports imports)
	{
		Type type = ItemType;
		if (type == null)
		{
			type = MemberType;
		}
		return model.GetSchemaTypeName(type, DataFormat, applyNetObjectProxy && AsReference, applyNetObjectProxy && DynamicType, ref imports);
	}
}
