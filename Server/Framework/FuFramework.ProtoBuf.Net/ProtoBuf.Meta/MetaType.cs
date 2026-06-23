using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta;

/// <summary>
/// Represents a type at runtime for use with protobuf, allowing the field mappings (etc) to be defined
/// </summary>
public class MetaType : ISerializerProxy
{
	internal sealed class Comparer : IComparer, IComparer<MetaType>
	{
		public static readonly Comparer Default = new Comparer();

		public int Compare(object x, object y)
		{
			return Compare(x as MetaType, y as MetaType);
		}

		public int Compare(MetaType x, MetaType y)
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
			return string.Compare(x.GetSchemaTypeName(), y.GetSchemaTypeName(), StringComparison.Ordinal);
		}
	}

	[Flags]
	internal enum AttributeFamily
	{
		None = 0,
		ProtoBuf = 1,
		DataContractSerialier = 2,
		XmlSerializer = 4,
		AutoTuple = 8
	}

	private BasicList subTypes;

	internal static readonly Type ienumerable = typeof(IEnumerable);

	private CallbackSet callbacks;

	private string name;

	private MethodInfo factory;

	private readonly RuntimeTypeModel model;

	private IProtoTypeSerializer serializer;

	private Type constructType;

	private Type surrogate;

	private readonly BasicList fields = new BasicList();

	private const ushort OPTIONS_Pending = 1;

	private const ushort OPTIONS_EnumPassThru = 2;

	private const ushort OPTIONS_Frozen = 4;

	private const ushort OPTIONS_PrivateOnApi = 8;

	private const ushort OPTIONS_SkipConstructor = 16;

	private const ushort OPTIONS_AsReferenceDefault = 32;

	private const ushort OPTIONS_AutoTuple = 64;

	private const ushort OPTIONS_IgnoreListHandling = 128;

	private const ushort OPTIONS_IsGroup = 256;

	private volatile ushort flags;

	IProtoSerializer ISerializerProxy.Serializer => Serializer;

	/// <summary>
	/// Gets the base-type for this type
	/// </summary>
	public MetaType BaseType { get; private set; }

	internal TypeModel Model => model;

	/// <summary>
	/// When used to compile a model, should public serialization/deserialzation methods
	/// be included for this type?
	/// </summary>
	public bool IncludeSerializerMethod
	{
		get
		{
			return !HasFlag(8);
		}
		set
		{
			SetFlag(8, !value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Should this type be treated as a reference by default?
	/// </summary>
	public bool AsReferenceDefault
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
	/// Indicates whether the current type has defined callbacks
	/// </summary>
	public bool HasCallbacks
	{
		get
		{
			if (callbacks != null)
			{
				return callbacks.NonTrivial;
			}
			return false;
		}
	}

	/// <summary>
	/// Indicates whether the current type has defined subtypes
	/// </summary>
	public bool HasSubtypes
	{
		get
		{
			if (subTypes != null)
			{
				return subTypes.Count != 0;
			}
			return false;
		}
	}

	/// <summary>
	/// Returns the set of callbacks defined for this type
	/// </summary>
	public CallbackSet Callbacks
	{
		get
		{
			if (callbacks == null)
			{
				callbacks = new CallbackSet(this);
			}
			return callbacks;
		}
	}

	private bool IsValueType => Type.IsValueType;

	/// <summary>
	/// Gets or sets the name of this contract.
	/// </summary>
	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			ThrowIfFrozen();
			name = value;
		}
	}

	/// <summary>
	/// The runtime type that the meta-type represents
	/// </summary>
	public Type Type { get; }

	internal IProtoTypeSerializer Serializer
	{
		get
		{
			if (serializer == null)
			{
				int opaqueToken = 0;
				try
				{
					model.TakeLock(ref opaqueToken);
					if (serializer == null)
					{
						SetFlag(4, value: true, throwIfFrozen: false);
						serializer = BuildSerializer();
					}
				}
				finally
				{
					model.ReleaseLock(opaqueToken);
				}
			}
			return serializer;
		}
	}

	internal bool IsList => (IgnoreListHandling ? null : TypeModel.GetListItemType(model, Type)) != null;

	/// <summary>
	/// Gets or sets whether the type should use a parameterless constructor (the default),
	/// or whether the type should skip the constructor completely. This option is not supported
	/// on compact-framework.
	/// </summary>
	public bool UseConstructor
	{
		get
		{
			return !HasFlag(16);
		}
		set
		{
			SetFlag(16, !value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// The concrete type to create when a new instance of this type is needed; this may be useful when dealing
	/// with dynamic proxies, or with interface-based APIs
	/// </summary>
	public Type ConstructType
	{
		get
		{
			return constructType;
		}
		set
		{
			ThrowIfFrozen();
			constructType = value;
		}
	}

	/// <summary>
	/// Returns the ValueMember that matchs a given field number, or null if not found
	/// </summary>
	public ValueMember this[int fieldNumber]
	{
		get
		{
			BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ValueMember valueMember = (ValueMember)enumerator.Current;
				if (valueMember.FieldNumber == fieldNumber)
				{
					return valueMember;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// Returns the ValueMember that matchs a given member (property/field), or null if not found
	/// </summary>
	public ValueMember this[MemberInfo member]
	{
		get
		{
			if (member == null)
			{
				return null;
			}
			BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ValueMember valueMember = (ValueMember)enumerator.Current;
				if (valueMember.Member == member || valueMember.BackingMember == member)
				{
					return valueMember;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating that an enum should be treated directly as an int/short/etc, rather
	/// than enforcing .proto enum rules. This is useful *in particul* for [Flags] enums.
	/// </summary>
	public bool EnumPassthru
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
	/// Gets or sets a value indicating that this type should NOT be treated as a list, even if it has
	/// familiar list-like characteristics (enumerable, add, etc)
	/// </summary>
	public bool IgnoreListHandling
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

	internal bool Pending
	{
		get
		{
			return HasFlag(1);
		}
		set
		{
			SetFlag(1, value, throwIfFrozen: false);
		}
	}

	internal IEnumerable Fields => fields;

	internal bool IsAutoTuple => HasFlag(64);

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
			SetFlag(256, value, throwIfFrozen: true);
		}
	}

	/// <summary>
	/// Get the name of the type being represented
	/// </summary>
	public override string ToString()
	{
		return Type.ToString();
	}

	private bool IsValidSubType(Type subType)
	{
		return Type.IsAssignableFrom(subType);
	}

	/// <summary>
	/// Adds a known sub-type to the inheritance model
	/// </summary>
	public MetaType AddSubType(int fieldNumber, Type derivedType)
	{
		return AddSubType(fieldNumber, derivedType, DataFormat.Default);
	}

	/// <summary>
	/// Adds a known sub-type to the inheritance model
	/// </summary>
	public MetaType AddSubType(int fieldNumber, Type derivedType, DataFormat dataFormat)
	{
		if (derivedType == null)
		{
			throw new ArgumentNullException("derivedType");
		}
		if (fieldNumber < 1)
		{
			throw new ArgumentOutOfRangeException("fieldNumber");
		}
		if ((!Type.IsClass && !Type.IsInterface) || Type.IsSealed)
		{
			throw new InvalidOperationException("Sub-types can only be added to non-sealed classes");
		}
		if (!IsValidSubType(derivedType))
		{
			throw new ArgumentException(derivedType.Name + " is not a valid sub-type of " + Type.Name, "derivedType");
		}
		MetaType metaType = model[derivedType];
		ThrowIfFrozen();
		metaType.ThrowIfFrozen();
		SubType value = new SubType(fieldNumber, metaType, dataFormat);
		ThrowIfFrozen();
		metaType.SetBaseType(this);
		if (subTypes == null)
		{
			subTypes = new BasicList();
		}
		subTypes.Add(value);
		model.ResetKeyCache();
		return this;
	}

	private void SetBaseType(MetaType baseType)
	{
		if (baseType == null)
		{
			throw new ArgumentNullException("baseType");
		}
		if (BaseType == baseType)
		{
			return;
		}
		if (BaseType != null)
		{
			throw new InvalidOperationException("Type '" + BaseType.Type.FullName + "' can only participate in one inheritance hierarchy");
		}
		for (MetaType metaType = baseType; metaType != null; metaType = metaType.BaseType)
		{
			if (metaType == this)
			{
				throw new InvalidOperationException("Cyclic inheritance of '" + BaseType.Type.FullName + "' is not allowed");
			}
		}
		BaseType = baseType;
	}

	/// <summary>
	/// Assigns the callbacks to use during serialiation/deserialization.
	/// </summary>
	/// <param name="beforeSerialize">The method (or null) called before serialization begins.</param>
	/// <param name="afterSerialize">The method (or null) called when serialization is complete.</param>
	/// <param name="beforeDeserialize">The method (or null) called before deserialization begins (or when a new instance is created during deserialization).</param>
	/// <param name="afterDeserialize">The method (or null) called when deserialization is complete.</param>
	/// <returns>The set of callbacks.</returns>
	public MetaType SetCallbacks(MethodInfo beforeSerialize, MethodInfo afterSerialize, MethodInfo beforeDeserialize, MethodInfo afterDeserialize)
	{
		CallbackSet callbackSet = Callbacks;
		callbackSet.BeforeSerialize = beforeSerialize;
		callbackSet.AfterSerialize = afterSerialize;
		callbackSet.BeforeDeserialize = beforeDeserialize;
		callbackSet.AfterDeserialize = afterDeserialize;
		return this;
	}

	/// <summary>
	/// Assigns the callbacks to use during serialiation/deserialization.
	/// </summary>
	/// <param name="beforeSerialize">The name of the method (or null) called before serialization begins.</param>
	/// <param name="afterSerialize">The name of the method (or null) called when serialization is complete.</param>
	/// <param name="beforeDeserialize">The name of the method (or null) called before deserialization begins (or when a new instance is created during deserialization).</param>
	/// <param name="afterDeserialize">The name of the method (or null) called when deserialization is complete.</param>
	/// <returns>The set of callbacks.</returns>
	public MetaType SetCallbacks(string beforeSerialize, string afterSerialize, string beforeDeserialize, string afterDeserialize)
	{
		if (IsValueType)
		{
			throw new InvalidOperationException();
		}
		CallbackSet callbackSet = Callbacks;
		callbackSet.BeforeSerialize = ResolveMethod(beforeSerialize, instance: true);
		callbackSet.AfterSerialize = ResolveMethod(afterSerialize, instance: true);
		callbackSet.BeforeDeserialize = ResolveMethod(beforeDeserialize, instance: true);
		callbackSet.AfterDeserialize = ResolveMethod(afterDeserialize, instance: true);
		return this;
	}

	/// <summary>
	/// Returns the public Type name of this Type used in serialization
	/// </summary>
	public string GetSchemaTypeName()
	{
		if (surrogate != null)
		{
			return model[surrogate].GetSchemaTypeName();
		}
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}
		string text = Type.Name;
		if (Type.IsGenericType)
		{
			StringBuilder stringBuilder = new StringBuilder(text);
			int num = text.IndexOf('`');
			if (num >= 0)
			{
				stringBuilder.Length = num;
			}
			Type[] genericArguments = Type.GetGenericArguments();
			foreach (Type obj in genericArguments)
			{
				stringBuilder.Append('_');
				Type type = obj;
				MetaType metaType;
				if (model.GetKey(ref type) >= 0 && (metaType = model[type]) != null && metaType.surrogate == null)
				{
					stringBuilder.Append(metaType.GetSchemaTypeName());
				}
				else
				{
					stringBuilder.Append(type.Name);
				}
			}
			return stringBuilder.ToString();
		}
		return text;
	}

	/// <summary>
	/// Designate a factory-method to use to create instances of this type
	/// </summary>
	public MetaType SetFactory(MethodInfo factory)
	{
		model.VerifyFactory(factory, Type);
		ThrowIfFrozen();
		this.factory = factory;
		return this;
	}

	/// <summary>
	/// Designate a factory-method to use to create instances of this type
	/// </summary>
	public MetaType SetFactory(string factory)
	{
		return SetFactory(ResolveMethod(factory, instance: false));
	}

	private MethodInfo ResolveMethod(string name, bool instance)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (!instance)
		{
			return Helpers.GetStaticMethod(Type, name);
		}
		return Helpers.GetInstanceMethod(Type, name);
	}

	internal static Exception InbuiltType(Type type)
	{
		return new ArgumentException("Data of this type has inbuilt behaviour, and cannot be added to a model in this way: " + type.FullName);
	}

	internal MetaType(RuntimeTypeModel model, Type type, MethodInfo factory)
	{
		this.factory = factory;
		if (model == null)
		{
			throw new ArgumentNullException("model");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type.IsArray)
		{
			throw InbuiltType(type);
		}
		if (model.TryGetBasicTypeSerializer(type) != null)
		{
			throw InbuiltType(type);
		}
		Type = type;
		this.model = model;
		if (Helpers.IsEnum(type))
		{
			EnumPassthru = type.IsDefined(model.MapType(typeof(FlagsAttribute)), inherit: false);
		}
	}

	/// <summary>
	/// Throws an exception if the type has been made immutable
	/// </summary>
	protected internal void ThrowIfFrozen()
	{
		if ((flags & 4) != 0)
		{
			throw new InvalidOperationException("The type cannot be changed once a serializer has been generated for " + Type.FullName);
		}
	}

	private IProtoTypeSerializer BuildSerializer()
	{
		if (Helpers.IsEnum(Type))
		{
			return new TagDecorator(1, WireType.Variant, strict: false, new EnumSerializer(Type, GetEnumMap()));
		}
		Type itemType = (IgnoreListHandling ? null : TypeModel.GetListItemType(model, Type));
		if (itemType != null)
		{
			if (surrogate != null)
			{
				throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot use a surrogate");
			}
			if (subTypes != null && subTypes.Count != 0)
			{
				throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be subclassed");
			}
			Type defaultType = null;
			ResolveListTypes(model, Type, ref itemType, ref defaultType);
			ValueMember valueMember = new ValueMember(model, 1, Type, itemType, defaultType, DataFormat.Default);
			return new TypeSerializer(model, Type, new int[1] { 1 }, new IProtoSerializer[1] { valueMember.Serializer }, null, isRootType: true, useConstructor: true, null, constructType, factory);
		}
		if (surrogate != null)
		{
			MetaType metaType = model[surrogate];
			MetaType baseType;
			while ((baseType = metaType.BaseType) != null)
			{
				metaType = baseType;
			}
			return new SurrogateSerializer(model, Type, surrogate, metaType.Serializer);
		}
		if (IsAutoTuple)
		{
			MemberInfo[] mappedMembers;
			ConstructorInfo constructorInfo = ResolveTupleConstructor(Type, out mappedMembers);
			if (constructorInfo == null)
			{
				throw new InvalidOperationException();
			}
			return new TupleSerializer(model, constructorInfo, mappedMembers);
		}
		fields.Trim();
		int count = fields.Count;
		int num = ((subTypes != null) ? subTypes.Count : 0);
		int[] array = new int[count + num];
		IProtoSerializer[] array2 = new IProtoSerializer[count + num];
		int num2 = 0;
		if (num != 0)
		{
			BasicList.NodeEnumerator enumerator = subTypes.GetEnumerator();
			while (enumerator.MoveNext())
			{
				SubType subType = (SubType)enumerator.Current;
				if (!subType.DerivedType.IgnoreListHandling && model.MapType(ienumerable).IsAssignableFrom(subType.DerivedType.Type))
				{
					throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a subclass");
				}
				array[num2] = subType.FieldNumber;
				array2[num2++] = subType.Serializer;
			}
		}
		if (count != 0)
		{
			BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ValueMember valueMember2 = (ValueMember)enumerator.Current;
				array[num2] = valueMember2.FieldNumber;
				array2[num2++] = valueMember2.Serializer;
			}
		}
		BasicList basicList = null;
		for (MetaType baseType2 = BaseType; baseType2 != null; baseType2 = baseType2.BaseType)
		{
			MethodInfo methodInfo = (baseType2.HasCallbacks ? baseType2.Callbacks.BeforeDeserialize : null);
			if (methodInfo != null)
			{
				if (basicList == null)
				{
					basicList = new BasicList();
				}
				basicList.Add(methodInfo);
			}
		}
		MethodInfo[] array3 = null;
		if (basicList != null)
		{
			array3 = new MethodInfo[basicList.Count];
			basicList.CopyTo(array3, 0);
			Array.Reverse(array3);
		}
		return new TypeSerializer(model, Type, array, array2, array3, BaseType == null, UseConstructor, callbacks, constructType, factory);
	}

	private static Type GetBaseType(MetaType type)
	{
		return type.Type.BaseType;
	}

	internal static bool GetAsReferenceDefault(RuntimeTypeModel model, Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (Helpers.IsEnum(type))
		{
			return false;
		}
		AttributeMap[] array = AttributeMap.Create(model, type, inherit: false);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].AttributeType.FullName == "ProtoBuf.ProtoContractAttribute" && array[i].TryGet("AsReferenceDefault", out object value))
			{
				return (bool)value;
			}
		}
		return false;
	}

	internal void ApplyDefaultBehaviour()
	{
		TypeAddedEventArgs args = null;
		RuntimeTypeModel.OnBeforeApplyDefaultBehaviour(this, ref args);
		if (args == null || args.ApplyDefaultBehaviour)
		{
			ApplyDefaultBehaviourImpl();
		}
		RuntimeTypeModel.OnAfterApplyDefaultBehaviour(this, ref args);
	}

	internal void ApplyDefaultBehaviourImpl(bool inheritPropertyInfo = true)
	{
		Type baseType = GetBaseType(this);
		if (baseType != null && model.FindWithoutAdd(baseType) == null && GetContractFamily(model, baseType, null) != 0)
		{
			model.FindOrAddAuto(baseType, demand: true, addWithContractOnly: false);
		}
		AttributeMap[] array = AttributeMap.Create(model, Type, inherit: true);
		AttributeFamily attributeFamily = GetContractFamily(model, Type, array);
		if (attributeFamily == AttributeFamily.AutoTuple)
		{
			SetFlag(64, value: true, throwIfFrozen: true);
		}
		bool flag = !EnumPassthru && Helpers.IsEnum(Type);
		if (attributeFamily == AttributeFamily.None && !flag)
		{
			return;
		}
		bool flag2 = flag;
		BasicList basicList = null;
		BasicList basicList2 = null;
		int dataMemberOffset = 0;
		int num = 1;
		bool flag3 = model.InferTagFromNameDefault;
		ImplicitFields implicitFields = ImplicitFields.None;
		string text = null;
		foreach (AttributeMap attributeMap in array)
		{
			string fullName = attributeMap.AttributeType.FullName;
			object value;
			if (!flag && fullName == "ProtoBuf.ProtoIncludeAttribute")
			{
				int fieldNumber = 0;
				if (attributeMap.TryGet("tag", out value))
				{
					fieldNumber = (int)value;
				}
				DataFormat dataFormat = DataFormat.Default;
				if (attributeMap.TryGet("DataFormat", out value))
				{
					dataFormat = (DataFormat)(int)value;
				}
				Type type = null;
				try
				{
					if (attributeMap.TryGet("knownTypeName", out value))
					{
						type = model.GetType((string)value, Type.Assembly);
					}
					else if (attributeMap.TryGet("knownType", out value))
					{
						type = (Type)value;
					}
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException("Unable to resolve sub-type of: " + Type.FullName, innerException);
				}
				if (type == null)
				{
					throw new InvalidOperationException("Unable to resolve sub-type of: " + Type.FullName);
				}
				if (IsValidSubType(type))
				{
					AddSubType(fieldNumber, type, dataFormat);
				}
			}
			if (fullName == "ProtoBuf.ProtoPartialIgnoreAttribute" && attributeMap.TryGet("MemberName", out value) && value != null)
			{
				if (basicList == null)
				{
					basicList = new BasicList();
				}
				basicList.Add((string)value);
			}
			if (!flag && fullName == "ProtoBuf.ProtoPartialMemberAttribute")
			{
				if (basicList2 == null)
				{
					basicList2 = new BasicList();
				}
				basicList2.Add(attributeMap);
			}
			if (fullName == "ProtoBuf.ProtoContractAttribute")
			{
				if (attributeMap.TryGet("Name", out value))
				{
					text = (string)value;
				}
				if (Helpers.IsEnum(Type))
				{
					if (attributeMap.TryGet("EnumPassthruHasValue", publicOnly: false, out value) && (bool)value && attributeMap.TryGet("EnumPassthru", out value))
					{
						EnumPassthru = (bool)value;
						flag2 = false;
						if (EnumPassthru)
						{
							flag = false;
						}
					}
				}
				else
				{
					if (attributeMap.TryGet("DataMemberOffset", out value))
					{
						dataMemberOffset = (int)value;
					}
					if (attributeMap.TryGet("InferTagFromNameHasValue", publicOnly: false, out value) && (bool)value && attributeMap.TryGet("InferTagFromName", out value))
					{
						flag3 = (bool)value;
					}
					if (attributeMap.TryGet("ImplicitFields", out value) && value != null)
					{
						implicitFields = (ImplicitFields)(int)value;
					}
					if (attributeMap.TryGet("SkipConstructor", out value))
					{
						UseConstructor = !(bool)value;
					}
					if (attributeMap.TryGet("IgnoreListHandling", out value))
					{
						IgnoreListHandling = (bool)value;
					}
					if (attributeMap.TryGet("AsReferenceDefault", out value))
					{
						AsReferenceDefault = (bool)value;
					}
					if (attributeMap.TryGet("ImplicitFirstTag", out value) && (int)value > 0)
					{
						num = (int)value;
					}
					if (attributeMap.TryGet("IsGroup", out value))
					{
						IsGroup = (bool)value;
					}
					if (attributeMap.TryGet("Surrogate", out value))
					{
						SetSurrogate((Type)value);
					}
				}
			}
			if (fullName == "System.Runtime.Serialization.DataContractAttribute" && text == null && attributeMap.TryGet("Name", out value))
			{
				text = (string)value;
			}
			if (fullName == "System.Xml.Serialization.XmlTypeAttribute" && text == null && attributeMap.TryGet("TypeName", out value))
			{
				text = (string)value;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			Name = text;
		}
		if (implicitFields != 0)
		{
			attributeFamily &= AttributeFamily.ProtoBuf;
		}
		MethodInfo[] array2 = null;
		BasicList basicList3 = new BasicList();
		MemberInfo[] members = Type.GetMembers(flag ? (BindingFlags.Static | BindingFlags.Public) : (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
		bool hasConflictingEnumValue = false;
		MemberInfo[] array3 = members;
		foreach (MemberInfo memberInfo in array3)
		{
			if ((!inheritPropertyInfo && memberInfo.DeclaringType != Type) || memberInfo.IsDefined(model.MapType(typeof(ProtoIgnoreAttribute)), inherit: true) || (basicList != null && basicList.Contains(memberInfo.Name)))
			{
				continue;
			}
			bool forced = false;
			if (memberInfo is PropertyInfo propertyInfo)
			{
				if (flag)
				{
					continue;
				}
				MemberInfo backingMember = null;
				if (!propertyInfo.CanWrite)
				{
					string text2 = "<" + propertyInfo.Name + ">k__BackingField";
					MemberInfo[] array4 = members;
					foreach (MemberInfo memberInfo2 in array4)
					{
						if (memberInfo2 as FieldInfo != null && memberInfo2.Name == text2)
						{
							backingMember = memberInfo2;
							break;
						}
					}
				}
				Type effectiveType = propertyInfo.PropertyType;
				bool isPublic = Helpers.GetGetMethod(propertyInfo, nonPublic: false, allowInternal: false) != null;
				bool isField = false;
				ApplyDefaultBehaviour_AddMembers(model, attributeFamily, flag, basicList2, dataMemberOffset, flag3, implicitFields, basicList3, memberInfo, ref forced, isPublic, isField, ref effectiveType, ref hasConflictingEnumValue, backingMember);
			}
			else if (memberInfo is FieldInfo fieldInfo)
			{
				Type effectiveType = fieldInfo.FieldType;
				bool isPublic = fieldInfo.IsPublic;
				bool isField = true;
				if (!flag || fieldInfo.IsStatic)
				{
					ApplyDefaultBehaviour_AddMembers(model, attributeFamily, flag, basicList2, dataMemberOffset, flag3, implicitFields, basicList3, memberInfo, ref forced, isPublic, isField, ref effectiveType, ref hasConflictingEnumValue);
				}
			}
			else if (memberInfo is MethodInfo methodInfo && !flag)
			{
				AttributeMap[] array5 = AttributeMap.Create(model, methodInfo, inherit: false);
				if (array5 != null && array5.Length != 0)
				{
					CheckForCallback(methodInfo, array5, "ProtoBuf.ProtoBeforeSerializationAttribute", ref array2, 0);
					CheckForCallback(methodInfo, array5, "ProtoBuf.ProtoAfterSerializationAttribute", ref array2, 1);
					CheckForCallback(methodInfo, array5, "ProtoBuf.ProtoBeforeDeserializationAttribute", ref array2, 2);
					CheckForCallback(methodInfo, array5, "ProtoBuf.ProtoAfterDeserializationAttribute", ref array2, 3);
					CheckForCallback(methodInfo, array5, "System.Runtime.Serialization.OnSerializingAttribute", ref array2, 4);
					CheckForCallback(methodInfo, array5, "System.Runtime.Serialization.OnSerializedAttribute", ref array2, 5);
					CheckForCallback(methodInfo, array5, "System.Runtime.Serialization.OnDeserializingAttribute", ref array2, 6);
					CheckForCallback(methodInfo, array5, "System.Runtime.Serialization.OnDeserializedAttribute", ref array2, 7);
				}
			}
		}
		if (flag && flag2 && !hasConflictingEnumValue)
		{
			EnumPassthru = true;
		}
		ProtoMemberAttribute[] array6 = new ProtoMemberAttribute[basicList3.Count];
		basicList3.CopyTo(array6, 0);
		ProtoMemberAttribute[] array7;
		if (flag3 || implicitFields != 0)
		{
			Array.Sort(array6);
			int num2 = num;
			array7 = array6;
			foreach (ProtoMemberAttribute protoMemberAttribute in array7)
			{
				if (!protoMemberAttribute.TagIsPinned)
				{
					protoMemberAttribute.Rebase(num2++);
				}
			}
		}
		array7 = array6;
		foreach (ProtoMemberAttribute normalizedAttribute in array7)
		{
			ValueMember valueMember = ApplyDefaultBehaviour(flag, normalizedAttribute);
			if (valueMember != null)
			{
				Add(valueMember);
			}
		}
		if (array2 != null)
		{
			SetCallbacks(Coalesce(array2, 0, 4), Coalesce(array2, 1, 5), Coalesce(array2, 2, 6), Coalesce(array2, 3, 7));
		}
	}

	private static void ApplyDefaultBehaviour_AddMembers(TypeModel model, AttributeFamily family, bool isEnum, BasicList partialMembers, int dataMemberOffset, bool inferTagByName, ImplicitFields implicitMode, BasicList members, MemberInfo member, ref bool forced, bool isPublic, bool isField, ref Type effectiveType, ref bool hasConflictingEnumValue, MemberInfo backingMember = null)
	{
		switch (implicitMode)
		{
		case ImplicitFields.AllFields:
			if (isField)
			{
				forced = true;
			}
			break;
		case ImplicitFields.AllPublic:
			if (isPublic)
			{
				forced = true;
			}
			break;
		}
		if (effectiveType.IsSubclassOf(model.MapType(typeof(Delegate))))
		{
			effectiveType = null;
		}
		if (effectiveType != null)
		{
			ProtoMemberAttribute protoMemberAttribute = NormalizeProtoMember(model, member, family, forced, isEnum, partialMembers, dataMemberOffset, inferTagByName, ref hasConflictingEnumValue, backingMember);
			if (protoMemberAttribute != null)
			{
				members.Add(protoMemberAttribute);
			}
		}
	}

	private static MethodInfo Coalesce(MethodInfo[] arr, int x, int y)
	{
		MethodInfo methodInfo = arr[x];
		if (methodInfo == null)
		{
			methodInfo = arr[y];
		}
		return methodInfo;
	}

	internal static AttributeFamily GetContractFamily(RuntimeTypeModel model, Type type, AttributeMap[] attributes)
	{
		AttributeFamily attributeFamily = AttributeFamily.None;
		if (attributes == null)
		{
			attributes = AttributeMap.Create(model, type, inherit: false);
		}
		for (int i = 0; i < attributes.Length; i++)
		{
			switch (attributes[i].AttributeType.FullName)
			{
			case "ProtoBuf.ProtoContractAttribute":
			{
				bool value = false;
				GetFieldBoolean(ref value, attributes[i], "UseProtoMembersOnly");
				if (value)
				{
					return AttributeFamily.ProtoBuf;
				}
				attributeFamily |= AttributeFamily.ProtoBuf;
				break;
			}
			case "System.Xml.Serialization.XmlTypeAttribute":
				if (!model.AutoAddProtoContractTypesOnly)
				{
					attributeFamily |= AttributeFamily.XmlSerializer;
				}
				break;
			case "System.Runtime.Serialization.DataContractAttribute":
				if (!model.AutoAddProtoContractTypesOnly)
				{
					attributeFamily |= AttributeFamily.DataContractSerialier;
				}
				break;
			}
		}
		if (attributeFamily == AttributeFamily.None && ResolveTupleConstructor(type, out MemberInfo[] _) != null)
		{
			attributeFamily |= AttributeFamily.AutoTuple;
		}
		return attributeFamily;
	}

	internal static ConstructorInfo ResolveTupleConstructor(Type type, out MemberInfo[] mappedMembers)
	{
		mappedMembers = null;
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type.IsAbstract)
		{
			return null;
		}
		ConstructorInfo[] constructors = Helpers.GetConstructors(type, nonPublic: false);
		if (constructors.Length == 0 || (constructors.Length == 1 && constructors[0].GetParameters().Length == 0))
		{
			return null;
		}
		MemberInfo[] instanceFieldsAndProperties = Helpers.GetInstanceFieldsAndProperties(type, publicOnly: true);
		BasicList basicList = new BasicList();
		bool flag = type.Name.IndexOf("Tuple", StringComparison.OrdinalIgnoreCase) < 0;
		for (int i = 0; i < instanceFieldsAndProperties.Length; i++)
		{
			if (instanceFieldsAndProperties[i] is PropertyInfo propertyInfo)
			{
				if (!propertyInfo.CanRead)
				{
					return null;
				}
				if (flag && propertyInfo.CanWrite && Helpers.GetSetMethod(propertyInfo, nonPublic: false, allowInternal: false) != null)
				{
					return null;
				}
				basicList.Add(propertyInfo);
			}
			else if (instanceFieldsAndProperties[i] is FieldInfo fieldInfo)
			{
				if (flag && !fieldInfo.IsInitOnly)
				{
					return null;
				}
				basicList.Add(fieldInfo);
			}
		}
		if (basicList.Count == 0)
		{
			return null;
		}
		MemberInfo[] array = new MemberInfo[basicList.Count];
		basicList.CopyTo(array, 0);
		int[] array2 = new int[array.Length];
		int num = 0;
		ConstructorInfo result = null;
		mappedMembers = new MemberInfo[array2.Length];
		for (int j = 0; j < constructors.Length; j++)
		{
			ParameterInfo[] parameters = constructors[j].GetParameters();
			if (parameters.Length != array.Length)
			{
				continue;
			}
			for (int k = 0; k < array2.Length; k++)
			{
				array2[k] = -1;
			}
			for (int l = 0; l < parameters.Length; l++)
			{
				for (int m = 0; m < array.Length; m++)
				{
					if (string.Compare(parameters[l].Name, array[m].Name, StringComparison.OrdinalIgnoreCase) == 0 && !(Helpers.GetMemberType(array[m]) != parameters[l].ParameterType))
					{
						array2[l] = m;
					}
				}
			}
			bool flag2 = false;
			for (int n = 0; n < array2.Length; n++)
			{
				if (array2[n] < 0)
				{
					flag2 = true;
					break;
				}
				mappedMembers[n] = array[array2[n]];
			}
			if (!flag2)
			{
				num++;
				result = constructors[j];
			}
		}
		if (num != 1)
		{
			return null;
		}
		return result;
	}

	private static void CheckForCallback(MethodInfo method, AttributeMap[] attributes, string callbackTypeName, ref MethodInfo[] callbacks, int index)
	{
		for (int i = 0; i < attributes.Length; i++)
		{
			if (attributes[i].AttributeType.FullName == callbackTypeName)
			{
				if (callbacks == null)
				{
					callbacks = new MethodInfo[8];
				}
				else if (callbacks[index] != null)
				{
					Type reflectedType = method.ReflectedType;
					throw new ProtoException("Duplicate " + callbackTypeName + " callbacks on " + reflectedType.FullName);
				}
				callbacks[index] = method;
			}
		}
	}

	private static bool HasFamily(AttributeFamily value, AttributeFamily required)
	{
		return (value & required) == required;
	}

	private static ProtoMemberAttribute NormalizeProtoMember(TypeModel model, MemberInfo member, AttributeFamily family, bool forced, bool isEnum, BasicList partialMembers, int dataMemberOffset, bool inferByTagName, ref bool hasConflictingEnumValue, MemberInfo backingMember = null)
	{
		if (member == null || (family == AttributeFamily.None && !isEnum))
		{
			return null;
		}
		int value = int.MinValue;
		int num = ((!inferByTagName) ? 1 : (-1));
		string text = null;
		bool value2 = false;
		bool ignore = false;
		bool flag = false;
		bool value3 = false;
		bool value4 = false;
		bool value5 = false;
		bool value6 = false;
		bool tagIsPinned = false;
		bool value7 = false;
		DataFormat value8 = DataFormat.Default;
		if (isEnum)
		{
			forced = true;
		}
		AttributeMap[] attribs = AttributeMap.Create(model, member, inherit: true);
		if (isEnum)
		{
			AttributeMap attribute = GetAttribute(attribs, "ProtoBuf.ProtoIgnoreAttribute");
			if (attribute != null)
			{
				ignore = true;
			}
			else
			{
				attribute = GetAttribute(attribs, "ProtoBuf.ProtoEnumAttribute");
				value = Convert.ToInt32(((FieldInfo)member).GetRawConstantValue());
				if (attribute != null)
				{
					GetFieldName(ref text, attribute, "Name");
					if ((bool)Helpers.GetInstanceMethod(attribute.AttributeType, "HasValue").Invoke(attribute.Target, null) && attribute.TryGet("Value", out object value9))
					{
						if (value != (int)value9)
						{
							hasConflictingEnumValue = true;
						}
						value = (int)value9;
					}
				}
			}
			flag = true;
		}
		if (!ignore && !flag)
		{
			AttributeMap attribute = GetAttribute(attribs, "ProtoBuf.ProtoMemberAttribute");
			GetIgnore(ref ignore, attribute, attribs, "ProtoBuf.ProtoIgnoreAttribute");
			if (!ignore && attribute != null)
			{
				GetFieldNumber(ref value, attribute, "Tag");
				GetFieldName(ref text, attribute, "Name");
				GetFieldBoolean(ref value3, attribute, "IsRequired");
				GetFieldBoolean(ref value2, attribute, "IsPacked");
				GetFieldBoolean(ref value7, attribute, "OverwriteList");
				GetDataFormat(ref value8, attribute, "DataFormat");
				GetFieldBoolean(ref value5, attribute, "AsReferenceHasValue", publicOnly: false);
				if (value5)
				{
					value5 = GetFieldBoolean(ref value4, attribute, "AsReference", publicOnly: true);
				}
				GetFieldBoolean(ref value6, attribute, "DynamicType");
				flag = (tagIsPinned = value > 0);
			}
			if (!flag && partialMembers != null)
			{
				BasicList.NodeEnumerator enumerator = partialMembers.GetEnumerator();
				while (enumerator.MoveNext())
				{
					AttributeMap attributeMap = (AttributeMap)enumerator.Current;
					if (attributeMap.TryGet("MemberName", out object value10) && (string)value10 == member.Name)
					{
						GetFieldNumber(ref value, attributeMap, "Tag");
						GetFieldName(ref text, attributeMap, "Name");
						GetFieldBoolean(ref value3, attributeMap, "IsRequired");
						GetFieldBoolean(ref value2, attributeMap, "IsPacked");
						GetFieldBoolean(ref value7, attribute, "OverwriteList");
						GetDataFormat(ref value8, attributeMap, "DataFormat");
						GetFieldBoolean(ref value5, attribute, "AsReferenceHasValue", publicOnly: false);
						if (value5)
						{
							value5 = GetFieldBoolean(ref value4, attributeMap, "AsReference", publicOnly: true);
						}
						GetFieldBoolean(ref value6, attributeMap, "DynamicType");
						if (flag = (tagIsPinned = value > 0))
						{
							break;
						}
					}
				}
			}
		}
		if (!ignore && !flag && HasFamily(family, AttributeFamily.DataContractSerialier))
		{
			AttributeMap attribute = GetAttribute(attribs, "System.Runtime.Serialization.DataMemberAttribute");
			if (attribute != null)
			{
				GetFieldNumber(ref value, attribute, "Order");
				GetFieldName(ref text, attribute, "Name");
				GetFieldBoolean(ref value3, attribute, "IsRequired");
				flag = value >= num;
				if (flag)
				{
					value += dataMemberOffset;
				}
			}
		}
		if (!ignore && !flag && HasFamily(family, AttributeFamily.XmlSerializer))
		{
			AttributeMap attribute = GetAttribute(attribs, "System.Xml.Serialization.XmlElementAttribute");
			if (attribute == null)
			{
				attribute = GetAttribute(attribs, "System.Xml.Serialization.XmlArrayAttribute");
			}
			GetIgnore(ref ignore, attribute, attribs, "System.Xml.Serialization.XmlIgnoreAttribute");
			if (attribute != null && !ignore)
			{
				GetFieldNumber(ref value, attribute, "Order");
				GetFieldName(ref text, attribute, "ElementName");
				flag = value >= num;
			}
		}
		if (!ignore && !flag && GetAttribute(attribs, "System.NonSerializedAttribute") != null)
		{
			ignore = true;
		}
		if (ignore || (value < num && !forced))
		{
			return null;
		}
		return new ProtoMemberAttribute(value, forced || inferByTagName)
		{
			AsReference = value4,
			AsReferenceHasValue = value5,
			DataFormat = value8,
			DynamicType = value6,
			IsPacked = value2,
			OverwriteList = value7,
			IsRequired = value3,
			Name = (string.IsNullOrEmpty(text) ? member.Name : text),
			Member = member,
			BackingMember = backingMember,
			TagIsPinned = tagIsPinned
		};
	}

	private ValueMember ApplyDefaultBehaviour(bool isEnum, ProtoMemberAttribute normalizedAttribute)
	{
		MemberInfo member;
		if (normalizedAttribute == null || (member = normalizedAttribute.Member) == null)
		{
			return null;
		}
		Type memberType = Helpers.GetMemberType(member);
		Type itemType = null;
		Type defaultType = null;
		ResolveListTypes(model, memberType, ref itemType, ref defaultType);
		bool flag = false;
		if (itemType != null && model.FindOrAddAuto(memberType, demand: false, addWithContractOnly: true) >= 0 && (flag = model[memberType].IgnoreListHandling))
		{
			itemType = null;
			defaultType = null;
		}
		AttributeMap[] attribs = AttributeMap.Create(model, member, inherit: true);
		object defaultValue = null;
		if (model.UseImplicitZeroDefaults)
		{
			switch (Helpers.GetTypeCode(memberType))
			{
			case ProtoTypeCode.Boolean:
				defaultValue = false;
				break;
			case ProtoTypeCode.Decimal:
				defaultValue = 0m;
				break;
			case ProtoTypeCode.Single:
				defaultValue = 0f;
				break;
			case ProtoTypeCode.Double:
				defaultValue = 0.0;
				break;
			case ProtoTypeCode.Byte:
				defaultValue = (byte)0;
				break;
			case ProtoTypeCode.Char:
				defaultValue = '\0';
				break;
			case ProtoTypeCode.Int16:
				defaultValue = (short)0;
				break;
			case ProtoTypeCode.Int32:
				defaultValue = 0;
				break;
			case ProtoTypeCode.Int64:
				defaultValue = 0L;
				break;
			case ProtoTypeCode.SByte:
				defaultValue = (sbyte)0;
				break;
			case ProtoTypeCode.UInt16:
				defaultValue = (ushort)0;
				break;
			case ProtoTypeCode.UInt32:
				defaultValue = 0u;
				break;
			case ProtoTypeCode.UInt64:
				defaultValue = 0uL;
				break;
			case ProtoTypeCode.TimeSpan:
				defaultValue = TimeSpan.Zero;
				break;
			case ProtoTypeCode.Guid:
				defaultValue = Guid.Empty;
				break;
			}
		}
		AttributeMap attribute;
		if ((attribute = GetAttribute(attribs, "System.ComponentModel.DefaultValueAttribute")) != null && attribute.TryGet("Value", out object value))
		{
			defaultValue = value;
		}
		ValueMember valueMember = ((isEnum || normalizedAttribute.Tag > 0) ? new ValueMember(model, Type, normalizedAttribute.Tag, member, memberType, itemType, defaultType, normalizedAttribute.DataFormat, defaultValue) : null);
		if (valueMember != null)
		{
			valueMember.BackingMember = normalizedAttribute.BackingMember;
			Type type = Type;
			PropertyInfo propertyInfo = Helpers.GetProperty(type, member.Name + "Specified", nonPublic: true);
			MethodInfo getMethod = Helpers.GetGetMethod(propertyInfo, nonPublic: true, allowInternal: true);
			if (getMethod == null || getMethod.IsStatic)
			{
				propertyInfo = null;
			}
			if (propertyInfo != null)
			{
				valueMember.SetSpecified(getMethod, Helpers.GetSetMethod(propertyInfo, nonPublic: true, allowInternal: true));
			}
			else
			{
				MethodInfo instanceMethod = Helpers.GetInstanceMethod(type, "ShouldSerialize" + member.Name, Helpers.EmptyTypes);
				if (instanceMethod != null && instanceMethod.ReturnType == model.MapType(typeof(bool)))
				{
					valueMember.SetSpecified(instanceMethod, null);
				}
			}
			if (!string.IsNullOrEmpty(normalizedAttribute.Name))
			{
				valueMember.SetName(normalizedAttribute.Name);
			}
			valueMember.IsPacked = normalizedAttribute.IsPacked;
			valueMember.IsRequired = normalizedAttribute.IsRequired;
			valueMember.OverwriteList = normalizedAttribute.OverwriteList;
			if (normalizedAttribute.AsReferenceHasValue)
			{
				valueMember.AsReference = normalizedAttribute.AsReference;
			}
			valueMember.DynamicType = normalizedAttribute.DynamicType;
			valueMember.IsMap = !flag && valueMember.ResolveMapTypes(out Type _, out Type _, out Type _);
			if (valueMember.IsMap && (attribute = GetAttribute(attribs, "ProtoBuf.ProtoMapAttribute")) != null)
			{
				if (attribute.TryGet("DisableMap", out object value2) && (bool)value2)
				{
					valueMember.IsMap = false;
				}
				else
				{
					if (attribute.TryGet("KeyFormat", out value2))
					{
						valueMember.MapKeyFormat = (DataFormat)value2;
					}
					if (attribute.TryGet("ValueFormat", out value2))
					{
						valueMember.MapValueFormat = (DataFormat)value2;
					}
				}
			}
		}
		return valueMember;
	}

	private static void GetDataFormat(ref DataFormat value, AttributeMap attrib, string memberName)
	{
		if (attrib != null && value == DataFormat.Default && attrib.TryGet(memberName, out object value2) && value2 != null)
		{
			value = (DataFormat)value2;
		}
	}

	private static void GetIgnore(ref bool ignore, AttributeMap attrib, AttributeMap[] attribs, string fullName)
	{
		if (!ignore && attrib != null)
		{
			ignore = GetAttribute(attribs, fullName) != null;
		}
	}

	private static void GetFieldBoolean(ref bool value, AttributeMap attrib, string memberName)
	{
		GetFieldBoolean(ref value, attrib, memberName, publicOnly: true);
	}

	private static bool GetFieldBoolean(ref bool value, AttributeMap attrib, string memberName, bool publicOnly)
	{
		if (attrib == null)
		{
			return false;
		}
		if (value)
		{
			return true;
		}
		if (attrib.TryGet(memberName, publicOnly, out object value2) && value2 != null)
		{
			value = (bool)value2;
			return true;
		}
		return false;
	}

	private static void GetFieldNumber(ref int value, AttributeMap attrib, string memberName)
	{
		if (attrib != null && value <= 0 && attrib.TryGet(memberName, out object value2) && value2 != null)
		{
			value = (int)value2;
		}
	}

	private static void GetFieldName(ref string name, AttributeMap attrib, string memberName)
	{
		if (attrib != null && string.IsNullOrEmpty(name) && attrib.TryGet(memberName, out object value) && value != null)
		{
			name = (string)value;
		}
	}

	private static AttributeMap GetAttribute(AttributeMap[] attribs, string fullName)
	{
		foreach (AttributeMap attributeMap in attribs)
		{
			if (attributeMap != null && attributeMap.AttributeType.FullName == fullName)
			{
				return attributeMap;
			}
		}
		return null;
	}

	/// <summary>
	/// Adds a member (by name) to the MetaType
	/// </summary>
	public MetaType Add(int fieldNumber, string memberName)
	{
		AddField(fieldNumber, memberName, null, null, null);
		return this;
	}

	/// <summary>
	/// Adds a member (by name) to the MetaType, returning the ValueMember rather than the fluent API.
	/// This is otherwise identical to Add.
	/// </summary>
	public ValueMember AddField(int fieldNumber, string memberName)
	{
		return AddField(fieldNumber, memberName, null, null, null);
	}

	/// <summary>
	/// Adds a member (by name) to the MetaType
	/// </summary>
	public MetaType Add(string memberName)
	{
		Add(GetNextFieldNumber(), memberName);
		return this;
	}

	/// <summary>
	/// Performs serialization of this type via a surrogate; all
	/// other serialization options are ignored and handled
	/// by the surrogate's configuration.
	/// </summary>
	public void SetSurrogate(Type surrogateType)
	{
		if (surrogateType == Type)
		{
			surrogateType = null;
		}
		if (surrogateType != null && surrogateType != null && Helpers.IsAssignableFrom(model.MapType(typeof(IEnumerable)), surrogateType))
		{
			throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a surrogate");
		}
		ThrowIfFrozen();
		surrogate = surrogateType;
	}

	internal MetaType GetSurrogateOrSelf()
	{
		if (surrogate != null)
		{
			return model[surrogate];
		}
		return this;
	}

	internal MetaType GetSurrogateOrBaseOrSelf(bool deep)
	{
		if (surrogate != null)
		{
			return model[surrogate];
		}
		MetaType baseType = BaseType;
		if (baseType != null)
		{
			if (deep)
			{
				MetaType result;
				do
				{
					result = baseType;
					baseType = baseType.BaseType;
				}
				while (baseType != null);
				return result;
			}
			return baseType;
		}
		return this;
	}

	private int GetNextFieldNumber()
	{
		int num = 0;
		BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ValueMember valueMember = (ValueMember)enumerator.Current;
			if (valueMember.FieldNumber > num)
			{
				num = valueMember.FieldNumber;
			}
		}
		if (subTypes != null)
		{
			enumerator = subTypes.GetEnumerator();
			while (enumerator.MoveNext())
			{
				SubType subType = (SubType)enumerator.Current;
				if (subType.FieldNumber > num)
				{
					num = subType.FieldNumber;
				}
			}
		}
		return num + 1;
	}

	/// <summary>
	/// Adds a set of members (by name) to the MetaType
	/// </summary>
	public MetaType Add(params string[] memberNames)
	{
		if (memberNames == null)
		{
			throw new ArgumentNullException("memberNames");
		}
		int nextFieldNumber = GetNextFieldNumber();
		for (int i = 0; i < memberNames.Length; i++)
		{
			Add(nextFieldNumber++, memberNames[i]);
		}
		return this;
	}

	/// <summary>
	/// Adds a member (by name) to the MetaType
	/// </summary>
	public MetaType Add(int fieldNumber, string memberName, object defaultValue)
	{
		AddField(fieldNumber, memberName, null, null, defaultValue);
		return this;
	}

	/// <summary>
	/// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists
	/// </summary>
	public MetaType Add(int fieldNumber, string memberName, Type itemType, Type defaultType)
	{
		AddField(fieldNumber, memberName, itemType, defaultType, null);
		return this;
	}

	/// <summary>
	/// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists, returning the ValueMember rather than the fluent API.
	/// This is otherwise identical to Add.
	/// </summary>
	public ValueMember AddField(int fieldNumber, string memberName, Type itemType, Type defaultType)
	{
		return AddField(fieldNumber, memberName, itemType, defaultType, null);
	}

	private ValueMember AddField(int fieldNumber, string memberName, Type itemType, Type defaultType, object defaultValue)
	{
		MemberInfo memberInfo = null;
		MemberInfo[] member = Type.GetMember(memberName, Helpers.IsEnum(Type) ? (BindingFlags.Static | BindingFlags.Public) : (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
		if (member != null && member.Length == 1)
		{
			memberInfo = member[0];
		}
		if (memberInfo == null)
		{
			throw new ArgumentException("Unable to determine member: " + memberName, "memberName");
		}
		PropertyInfo propertyInfo = null;
		Type type;
		switch (memberInfo.MemberType)
		{
		case MemberTypes.Field:
			type = ((FieldInfo)memberInfo).FieldType;
			break;
		case MemberTypes.Property:
			propertyInfo = (PropertyInfo)memberInfo;
			type = propertyInfo.PropertyType;
			break;
		default:
			throw new NotSupportedException(memberInfo.MemberType.ToString());
		}
		ResolveListTypes(model, type, ref itemType, ref defaultType);
		MemberInfo memberInfo2 = null;
		if ((object)propertyInfo != null && !propertyInfo.CanWrite)
		{
			_ = "<" + ((PropertyInfo)memberInfo).Name + ">k__BackingField";
			MemberInfo[] member2 = Type.GetMember("<" + ((PropertyInfo)memberInfo).Name + ">k__BackingField", Helpers.IsEnum(Type) ? (BindingFlags.Static | BindingFlags.Public) : (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
			if (member2 != null && member2.Length == 1 && member2[0] as FieldInfo != null)
			{
				memberInfo2 = member2[0];
			}
		}
		ValueMember valueMember = new ValueMember(model, Type, fieldNumber, memberInfo2 ?? memberInfo, type, itemType, defaultType, DataFormat.Default, defaultValue);
		if (memberInfo2 != null)
		{
			valueMember.SetName(memberInfo.Name);
		}
		Add(valueMember);
		return valueMember;
	}

	internal static void ResolveListTypes(TypeModel model, Type type, ref Type itemType, ref Type defaultType)
	{
		if (type == null)
		{
			return;
		}
		if (type.IsArray)
		{
			if (type.GetArrayRank() != 1)
			{
				throw new NotSupportedException("Multi-dimensional arrays are not supported");
			}
			itemType = type.GetElementType();
			if (itemType == model.MapType(typeof(byte)))
			{
				defaultType = (itemType = null);
			}
			else
			{
				defaultType = type;
			}
		}
		if (itemType == null)
		{
			itemType = TypeModel.GetListItemType(model, type);
		}
		if (itemType != null)
		{
			Type itemType2 = null;
			Type defaultType2 = null;
			ResolveListTypes(model, itemType, ref itemType2, ref defaultType2);
			if (itemType2 != null)
			{
				throw TypeModel.CreateNestedListsNotSupported(type);
			}
		}
		if (!(itemType != null) || !(defaultType == null))
		{
			return;
		}
		if (type.IsClass && !type.IsAbstract && Helpers.GetConstructor(type, Helpers.EmptyTypes, nonPublic: true) != null)
		{
			defaultType = type;
		}
		if (defaultType == null && type.IsInterface)
		{
			Type[] genericArguments;
			if (type.IsGenericType && type.GetGenericTypeDefinition() == model.MapType(typeof(IDictionary<, >)) && itemType == model.MapType(typeof(KeyValuePair<, >)).MakeGenericType(genericArguments = type.GetGenericArguments()))
			{
				defaultType = model.MapType(typeof(Dictionary<, >)).MakeGenericType(genericArguments);
			}
			else
			{
				defaultType = model.MapType(typeof(List<>)).MakeGenericType(itemType);
			}
		}
		if (defaultType != null && !Helpers.IsAssignableFrom(type, defaultType))
		{
			defaultType = null;
		}
	}

	private void Add(ValueMember member)
	{
		int opaqueToken = 0;
		try
		{
			model.TakeLock(ref opaqueToken);
			ThrowIfFrozen();
			fields.Add(member);
		}
		finally
		{
			model.ReleaseLock(opaqueToken);
		}
	}

	/// <summary>
	/// Returns the ValueMember instances associated with this type
	/// </summary>
	public ValueMember[] GetFields()
	{
		ValueMember[] array = new ValueMember[fields.Count];
		fields.CopyTo(array, 0);
		Array.Sort(array, ValueMember.Comparer.Default);
		return array;
	}

	/// <summary>
	/// Returns the SubType instances associated with this type
	/// </summary>
	public SubType[] GetSubtypes()
	{
		if (subTypes == null || subTypes.Count == 0)
		{
			return new SubType[0];
		}
		SubType[] array = new SubType[subTypes.Count];
		subTypes.CopyTo(array, 0);
		Array.Sort(array, SubType.Comparer.Default);
		return array;
	}

	internal IEnumerable<Type> GetAllGenericArguments()
	{
		return GetAllGenericArguments(Type);
	}

	private static IEnumerable<Type> GetAllGenericArguments(Type type)
	{
		Type[] genericArguments = type.GetGenericArguments();
		Type[] array = genericArguments;
		foreach (Type arg in array)
		{
			yield return arg;
			foreach (Type allGenericArgument in GetAllGenericArguments(arg))
			{
				yield return allGenericArgument;
			}
		}
	}

	internal bool IsDefined(int fieldNumber)
	{
		BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (((ValueMember)enumerator.Current).FieldNumber == fieldNumber)
			{
				return true;
			}
		}
		return false;
	}

	internal int GetKey(bool demand, bool getBaseKey)
	{
		return model.GetKey(Type, demand, getBaseKey);
	}

	internal EnumSerializer.EnumPair[] GetEnumMap()
	{
		if (HasFlag(2))
		{
			return null;
		}
		EnumSerializer.EnumPair[] array = new EnumSerializer.EnumPair[fields.Count];
		for (int i = 0; i < array.Length; i++)
		{
			ValueMember valueMember = (ValueMember)fields[i];
			int fieldNumber = valueMember.FieldNumber;
			object rawEnumValue = valueMember.GetRawEnumValue();
			array[i] = new EnumSerializer.EnumPair(fieldNumber, rawEnumValue, valueMember.MemberType);
		}
		return array;
	}

	private bool HasFlag(ushort flag)
	{
		return (flags & flag) == flag;
	}

	private void SetFlag(ushort flag, bool value, bool throwIfFrozen)
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
			flags = (ushort)(flags & ~flag);
		}
	}

	internal static MetaType GetRootType(MetaType source)
	{
		while (source.serializer != null)
		{
			MetaType baseType = source.BaseType;
			if (baseType == null)
			{
				return source;
			}
			source = baseType;
		}
		RuntimeTypeModel runtimeTypeModel = source.model;
		int opaqueToken = 0;
		try
		{
			runtimeTypeModel.TakeLock(ref opaqueToken);
			MetaType baseType2;
			while ((baseType2 = source.BaseType) != null)
			{
				source = baseType2;
			}
			return source;
		}
		finally
		{
			runtimeTypeModel.ReleaseLock(opaqueToken);
		}
	}

	internal bool IsPrepared()
	{
		return false;
	}

	internal static StringBuilder NewLine(StringBuilder builder, int indent)
	{
		return Helpers.AppendLine(builder).Append(' ', indent * 3);
	}

	internal void WriteSchema(StringBuilder builder, int indent, ref RuntimeTypeModel.CommonImports imports, ProtoSyntax syntax)
	{
		if (surrogate != null)
		{
			return;
		}
		ValueMember[] array = new ValueMember[fields.Count];
		fields.CopyTo(array, 0);
		Array.Sort(array, ValueMember.Comparer.Default);
		if (IsList)
		{
			string schemaTypeName = model.GetSchemaTypeName(TypeModel.GetListItemType(model, Type), DataFormat.Default, asReference: false, dynamicType: false, ref imports);
			NewLine(builder, indent).Append("message ").Append(GetSchemaTypeName()).Append(" {");
			NewLine(builder, indent + 1).Append("repeated ").Append(schemaTypeName).Append(" items = 1;");
			NewLine(builder, indent).Append('}');
			return;
		}
		if (IsAutoTuple)
		{
			if (!(ResolveTupleConstructor(Type, out MemberInfo[] mappedMembers) != null))
			{
				return;
			}
			NewLine(builder, indent).Append("message ").Append(GetSchemaTypeName()).Append(" {");
			for (int i = 0; i < mappedMembers.Length; i++)
			{
				Type effectiveType;
				if (mappedMembers[i] is PropertyInfo propertyInfo)
				{
					effectiveType = propertyInfo.PropertyType;
				}
				else
				{
					if (!(mappedMembers[i] is FieldInfo fieldInfo))
					{
						throw new NotSupportedException("Unknown member type: " + mappedMembers[i].GetType().Name);
					}
					effectiveType = fieldInfo.FieldType;
				}
				NewLine(builder, indent + 1).Append((syntax == ProtoSyntax.Proto2) ? "optional " : "").Append(model.GetSchemaTypeName(effectiveType, DataFormat.Default, asReference: false, dynamicType: false, ref imports).Replace('.', '_')).Append(' ')
					.Append(mappedMembers[i].Name)
					.Append(" = ")
					.Append(i + 1)
					.Append(';');
			}
			NewLine(builder, indent).Append('}');
			return;
		}
		ValueMember[] array3;
		if (Helpers.IsEnum(Type))
		{
			NewLine(builder, indent).Append("enum ").Append(GetSchemaTypeName()).Append(" {");
			if (array.Length == 0 && EnumPassthru)
			{
				if (Type.IsDefined(model.MapType(typeof(FlagsAttribute)), inherit: false))
				{
					NewLine(builder, indent + 1).Append("// this is a composite/flags enumeration");
				}
				else
				{
					NewLine(builder, indent + 1).Append("// this enumeration will be passed as a raw value");
				}
				FieldInfo[] array2 = Type.GetFields();
				foreach (FieldInfo fieldInfo2 in array2)
				{
					if (fieldInfo2.IsStatic && fieldInfo2.IsLiteral)
					{
						object rawConstantValue = fieldInfo2.GetRawConstantValue();
						NewLine(builder, indent + 1).Append(fieldInfo2.Name).Append(" = ").Append(rawConstantValue)
							.Append(";");
					}
				}
			}
			else
			{
				Dictionary<int, int> dictionary = new Dictionary<int, int>(array.Length);
				bool flag = false;
				array3 = array;
				foreach (ValueMember valueMember in array3)
				{
					if (dictionary.ContainsKey(valueMember.FieldNumber))
					{
						flag = true;
						break;
					}
					dictionary.Add(valueMember.FieldNumber, 1);
				}
				if (flag)
				{
					NewLine(builder, indent + 1).Append("option allow_alias = true;");
				}
				bool flag2 = false;
				array3 = array;
				foreach (ValueMember valueMember2 in array3)
				{
					if (valueMember2.FieldNumber == 0)
					{
						NewLine(builder, indent + 1).Append(valueMember2.Name).Append(" = ").Append(valueMember2.FieldNumber)
							.Append(';');
						flag2 = true;
					}
				}
				if (syntax == ProtoSyntax.Proto3 && !flag2)
				{
					NewLine(builder, indent + 1).Append("ZERO = 0; // proto3 requires a zero value as the first item (it can be named anything)");
				}
				array3 = array;
				foreach (ValueMember valueMember3 in array3)
				{
					if (valueMember3.FieldNumber != 0)
					{
						NewLine(builder, indent + 1).Append(valueMember3.Name).Append(" = ").Append(valueMember3.FieldNumber)
							.Append(';');
					}
				}
			}
			NewLine(builder, indent).Append('}');
			return;
		}
		NewLine(builder, indent).Append("message ").Append(GetSchemaTypeName()).Append(" {");
		array3 = array;
		foreach (ValueMember valueMember4 in array3)
		{
			bool hasOption = false;
			string schemaTypeName3;
			if (valueMember4.IsMap)
			{
				valueMember4.ResolveMapTypes(out Type _, out Type keyType, out Type valueType);
				string schemaTypeName2 = model.GetSchemaTypeName(keyType, valueMember4.MapKeyFormat, asReference: false, dynamicType: false, ref imports);
				schemaTypeName3 = model.GetSchemaTypeName(valueType, valueMember4.MapKeyFormat, valueMember4.AsReference, valueMember4.DynamicType, ref imports);
				NewLine(builder, indent + 1).Append("map<").Append(schemaTypeName2).Append(",")
					.Append(schemaTypeName3)
					.Append("> ")
					.Append(valueMember4.Name)
					.Append(" = ")
					.Append(valueMember4.FieldNumber)
					.Append(";");
			}
			else
			{
				string value = ((valueMember4.ItemType != null) ? "repeated " : ((syntax != 0) ? "" : (valueMember4.IsRequired ? "required " : "optional ")));
				NewLine(builder, indent + 1).Append(value);
				if (valueMember4.DataFormat == DataFormat.Group)
				{
					builder.Append("group ");
				}
				schemaTypeName3 = valueMember4.GetSchemaTypeName(applyNetObjectProxy: true, ref imports);
				builder.Append(schemaTypeName3).Append(" ").Append(valueMember4.Name)
					.Append(" = ")
					.Append(valueMember4.FieldNumber);
				if (syntax == ProtoSyntax.Proto2 && valueMember4.DefaultValue != null && !valueMember4.IsRequired)
				{
					if (valueMember4.DefaultValue is string)
					{
						AddOption(builder, ref hasOption).Append("default = \"").Append(valueMember4.DefaultValue).Append("\"");
					}
					else if (!(valueMember4.DefaultValue is TimeSpan))
					{
						if (valueMember4.DefaultValue is bool)
						{
							AddOption(builder, ref hasOption).Append(((bool)valueMember4.DefaultValue) ? "default = true" : "default = false");
						}
						else
						{
							AddOption(builder, ref hasOption).Append("default = ").Append(valueMember4.DefaultValue);
						}
					}
				}
				if (CanPack(valueMember4.ItemType))
				{
					if (syntax == ProtoSyntax.Proto2)
					{
						if (valueMember4.IsPacked)
						{
							AddOption(builder, ref hasOption).Append("packed = true");
						}
					}
					else if (!valueMember4.IsPacked)
					{
						AddOption(builder, ref hasOption).Append("packed = false");
					}
				}
				if (valueMember4.AsReference)
				{
					imports |= RuntimeTypeModel.CommonImports.Protogen;
					AddOption(builder, ref hasOption).Append("(.protobuf_net.fieldopt).asRef = true");
				}
				if (valueMember4.DynamicType)
				{
					imports |= RuntimeTypeModel.CommonImports.Protogen;
					AddOption(builder, ref hasOption).Append("(.protobuf_net.fieldopt).dynamicType = true");
				}
				CloseOption(builder, ref hasOption).Append(';');
				if (syntax != 0 && valueMember4.DefaultValue != null && !valueMember4.IsRequired && !IsImplicitDefault(valueMember4.DefaultValue))
				{
					builder.Append(" // default value could not be applied: ").Append(valueMember4.DefaultValue);
				}
			}
			if (schemaTypeName3 == ".bcl.NetObjectProxy" && valueMember4.AsReference && !valueMember4.DynamicType)
			{
				builder.Append(" // reference-tracked ").Append(valueMember4.GetSchemaTypeName(applyNetObjectProxy: false, ref imports));
			}
		}
		if (subTypes != null && subTypes.Count != 0)
		{
			SubType[] array4 = new SubType[subTypes.Count];
			subTypes.CopyTo(array4, 0);
			Array.Sort(array4, SubType.Comparer.Default);
			string[] array5 = new string[array4.Length];
			for (int k = 0; k < array4.Length; k++)
			{
				array5[k] = array4[k].DerivedType.GetSchemaTypeName();
			}
			string text = "subtype";
			while (Array.IndexOf(array5, text) >= 0)
			{
				text = "_" + text;
			}
			NewLine(builder, indent + 1).Append("oneof ").Append(text).Append(" {");
			for (int l = 0; l < array4.Length; l++)
			{
				string value2 = array5[l];
				NewLine(builder, indent + 2).Append(value2).Append(" ").Append(value2)
					.Append(" = ")
					.Append(array4[l].FieldNumber)
					.Append(';');
			}
			NewLine(builder, indent + 1).Append("}");
		}
		NewLine(builder, indent).Append('}');
	}

	private static StringBuilder AddOption(StringBuilder builder, ref bool hasOption)
	{
		if (hasOption)
		{
			return builder.Append(", ");
		}
		hasOption = true;
		return builder.Append(" [");
	}

	private static StringBuilder CloseOption(StringBuilder builder, ref bool hasOption)
	{
		if (hasOption)
		{
			hasOption = false;
			return builder.Append("]");
		}
		return builder;
	}

	private static bool IsImplicitDefault(object value)
	{
		try
		{
			if (value == null)
			{
				return false;
			}
			switch (Helpers.GetTypeCode(value.GetType()))
			{
			case ProtoTypeCode.Boolean:
				return !(bool)value;
			case ProtoTypeCode.Byte:
				return (byte)value == 0;
			case ProtoTypeCode.Char:
				return (char)value == '\0';
			case ProtoTypeCode.DateTime:
				return (DateTime)value == default(DateTime);
			case ProtoTypeCode.Decimal:
				return (decimal)value == 0m;
			case ProtoTypeCode.Double:
				return (double)value == 0.0;
			case ProtoTypeCode.Int16:
				return (short)value == 0;
			case ProtoTypeCode.Int32:
				return (int)value == 0;
			case ProtoTypeCode.Int64:
				return (long)value == 0;
			case ProtoTypeCode.SByte:
				return (sbyte)value == 0;
			case ProtoTypeCode.Single:
				return (float)value == 0f;
			case ProtoTypeCode.String:
				return (string)value == "";
			case ProtoTypeCode.TimeSpan:
				return (TimeSpan)value == TimeSpan.Zero;
			case ProtoTypeCode.UInt16:
				return (ushort)value == 0;
			case ProtoTypeCode.UInt32:
				return (uint)value == 0;
			case ProtoTypeCode.UInt64:
				return (ulong)value == 0;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool CanPack(Type type)
	{
		if (type == null)
		{
			return false;
		}
		ProtoTypeCode typeCode = Helpers.GetTypeCode(type);
		if ((uint)(typeCode - 3) <= 11u)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Apply a shift to all fields (and sub-types) on this type
	/// </summary>
	/// <param name="offset">The change in field number to apply</param>
	/// <remarks>The resultant field numbers must still all be considered valid</remarks>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public void ApplyFieldOffset(int offset)
	{
		if (Helpers.IsEnum(Type))
		{
			throw new InvalidOperationException("Cannot apply field-offset to an enum");
		}
		if (offset == 0)
		{
			return;
		}
		int opaqueToken = 0;
		try
		{
			model.TakeLock(ref opaqueToken);
			ThrowIfFrozen();
			if (fields != null)
			{
				BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
				while (enumerator.MoveNext())
				{
					AssertValidFieldNumber(((ValueMember)enumerator.Current).FieldNumber + offset);
				}
			}
			if (subTypes != null)
			{
				BasicList.NodeEnumerator enumerator = subTypes.GetEnumerator();
				while (enumerator.MoveNext())
				{
					AssertValidFieldNumber(((SubType)enumerator.Current).FieldNumber + offset);
				}
			}
			if (fields != null)
			{
				BasicList.NodeEnumerator enumerator = fields.GetEnumerator();
				while (enumerator.MoveNext())
				{
					((ValueMember)enumerator.Current).FieldNumber += offset;
				}
			}
			if (subTypes != null)
			{
				BasicList.NodeEnumerator enumerator = subTypes.GetEnumerator();
				while (enumerator.MoveNext())
				{
					((SubType)enumerator.Current).FieldNumber += offset;
				}
			}
		}
		finally
		{
			model.ReleaseLock(opaqueToken);
		}
	}

	internal static void AssertValidFieldNumber(int fieldNumber)
	{
		if (fieldNumber < 1)
		{
			throw new ArgumentOutOfRangeException("fieldNumber");
		}
	}
}
