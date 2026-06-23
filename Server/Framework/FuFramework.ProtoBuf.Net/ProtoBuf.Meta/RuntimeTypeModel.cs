using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta;

/// <summary>
/// Provides protobuf serialization support for a number of types that can be defined at runtime
/// </summary>
public sealed class RuntimeTypeModel : TypeModel
{
	private sealed class Singleton
	{
		internal static readonly RuntimeTypeModel Value = new RuntimeTypeModel(isDefault: true);

		private Singleton()
		{
		}
	}

	[Flags]
	internal enum CommonImports
	{
		None = 0,
		Bcl = 1,
		Timestamp = 2,
		Duration = 4,
		Protogen = 8
	}

	private sealed class BasicType
	{
		public Type Type { get; }

		public IProtoSerializer Serializer { get; }

		public BasicType(Type type, IProtoSerializer serializer)
		{
			Type = type;
			Serializer = serializer;
		}
	}

	private ushort options;

	private const ushort OPTIONS_InferTagFromNameDefault = 1;

	private const ushort OPTIONS_IsDefaultModel = 2;

	private const ushort OPTIONS_Frozen = 4;

	private const ushort OPTIONS_AutoAddMissingTypes = 8;

	private const ushort OPTIONS_UseImplicitZeroDefaults = 32;

	private const ushort OPTIONS_AllowParseableTypes = 64;

	private const ushort OPTIONS_AutoAddProtoContractTypesOnly = 128;

	private const ushort OPTIONS_IncludeDateTimeKind = 256;

	private const ushort OPTIONS_DoNotInternStrings = 512;

	private static readonly BasicList.MatchPredicate MetaTypeFinder = MetaTypeFinderImpl;

	private static readonly BasicList.MatchPredicate BasicTypeFinder = BasicTypeFinderImpl;

	private readonly BasicList basicTypes = new BasicList();

	private readonly BasicList types = new BasicList();

	private int metadataTimeoutMilliseconds = 5000;

	private int contentionCounter = 1;

	private MethodInfo defaultFactory;

	/// <summary>
	/// Global default that
	/// enables/disables automatic tag generation based on the existing name / order
	/// of the defined members. See <seealso cref="P:ProtoBuf.ProtoContractAttribute.InferTagFromName" />
	/// for usage and <b>important warning</b> / explanation.
	/// You must set the global default before attempting to serialize/deserialize any
	/// impacted type.
	/// </summary>
	public bool InferTagFromNameDefault
	{
		get
		{
			return GetOption(1);
		}
		set
		{
			SetOption(1, value);
		}
	}

	/// <summary>
	/// Global default that determines whether types are considered serializable
	/// if they have [DataContract] / [XmlType]. With this enabled, <b>ONLY</b>
	/// types marked as [ProtoContract] are added automatically.
	/// </summary>
	public bool AutoAddProtoContractTypesOnly
	{
		get
		{
			return GetOption(128);
		}
		set
		{
			SetOption(128, value);
		}
	}

	/// <summary>
	/// Global switch that enables or disables the implicit
	/// handling of "zero defaults"; meanning: if no other default is specified,
	/// it assumes bools always default to false, integers to zero, etc.
	/// If this is disabled, no such assumptions are made and only *explicit*
	/// default values are processed. This is enabled by default to
	/// preserve similar logic to v1.
	/// </summary>
	public bool UseImplicitZeroDefaults
	{
		get
		{
			return GetOption(32);
		}
		set
		{
			if (!value && GetOption(2))
			{
				throw new InvalidOperationException("UseImplicitZeroDefaults cannot be disabled on the default model");
			}
			SetOption(32, value);
		}
	}

	/// <summary>
	/// Global switch that determines whether types with a <c>.ToString()</c> and a <c>Parse(string)</c>
	/// should be serialized as strings.
	/// </summary>
	public bool AllowParseableTypes
	{
		get
		{
			return GetOption(64);
		}
		set
		{
			SetOption(64, value);
		}
	}

	/// <summary>
	/// Global switch that determines whether DateTime serialization should include the <c>Kind</c> of the date/time.
	/// </summary>
	public bool IncludeDateTimeKind
	{
		get
		{
			return GetOption(256);
		}
		set
		{
			SetOption(256, value);
		}
	}

	/// <summary>
	/// Global switch that determines whether a single instance of the same string should be used during deserialization.
	/// </summary>
	/// <remarks>Note this does not use the global .NET string interner</remarks>
	public bool InternStrings
	{
		get
		{
			return !GetOption(512);
		}
		set
		{
			SetOption(512, !value);
		}
	}

	/// <summary>
	/// The default model, used to support ProtoBuf.Serializer
	/// </summary>
	public static RuntimeTypeModel Default => Singleton.Value;

	/// <summary>
	/// Obtains the MetaType associated with a given Type for the current model,
	/// allowing additional configuration.
	/// </summary>
	public MetaType this[Type type] => (MetaType)types[FindOrAddAuto(type, demand: true, addWithContractOnly: false)];

	/// <summary>
	/// Should support for unexpected types be added automatically?
	/// If false, an exception is thrown when unexpected types
	/// are encountered.
	/// </summary>
	public bool AutoAddMissingTypes
	{
		get
		{
			return GetOption(8);
		}
		set
		{
			if (!value && GetOption(2))
			{
				throw new InvalidOperationException("The default model must allow missing types");
			}
			ThrowIfFrozen();
			SetOption(8, value);
		}
	}

	/// <summary>
	/// The amount of time to wait if there are concurrent metadata access operations
	/// </summary>
	public int MetadataTimeoutMilliseconds
	{
		get
		{
			return metadataTimeoutMilliseconds;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("MetadataTimeoutMilliseconds");
			}
			metadataTimeoutMilliseconds = value;
		}
	}

	/// <summary>
	/// If a lock-contention is detected, this event signals the *owner* of the lock responsible for the blockage, indicating
	/// what caused the problem; this is only raised if the lock-owning code successfully completes.
	/// </summary>
	public event LockContentedEventHandler LockContended;

	/// <summary>
	/// Raised before a type is auto-configured; this allows the auto-configuration to be electively suppressed
	/// </summary>
	/// <remarks>This callback should be fast and not involve complex external calls, as it may block the model</remarks>
	public event EventHandler<TypeAddedEventArgs> BeforeApplyDefaultBehaviour;

	/// <summary>
	/// Raised after a type is auto-configured; this allows additional external customizations
	/// </summary>
	/// <remarks>This callback should be fast and not involve complex external calls, as it may block the model</remarks>
	public event EventHandler<TypeAddedEventArgs> AfterApplyDefaultBehaviour;

	private bool GetOption(ushort option)
	{
		return (options & option) == option;
	}

	private void SetOption(ushort option, bool value)
	{
		if (value)
		{
			options |= option;
		}
		else
		{
			options &= (ushort)(~option);
		}
	}

	/// <summary>
	/// Should the <c>Kind</c> be included on date/time values?
	/// </summary>
	protected internal override bool SerializeDateTimeKind()
	{
		return GetOption(256);
	}

	/// <summary>
	/// Returns a sequence of the Type instances that can be
	/// processed by this model.
	/// </summary>
	public IEnumerable GetTypes()
	{
		return types;
	}

	/// <summary>
	/// Suggest a .proto definition for the given type
	/// </summary>
	/// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
	/// <returns>The .proto definition as a string</returns>
	/// <param name="syntax">The .proto syntax to use</param>
	public override string GetSchema(Type type, ProtoSyntax syntax)
	{
		BasicList basicList = new BasicList();
		MetaType metaType = null;
		bool flag = false;
		if (type == null)
		{
			BasicList.NodeEnumerator enumerator = types.GetEnumerator();
			while (enumerator.MoveNext())
			{
				MetaType surrogateOrBaseOrSelf = ((MetaType)enumerator.Current).GetSurrogateOrBaseOrSelf(deep: false);
				if (!basicList.Contains(surrogateOrBaseOrSelf))
				{
					basicList.Add(surrogateOrBaseOrSelf);
					CascadeDependents(basicList, surrogateOrBaseOrSelf);
				}
			}
		}
		else
		{
			Type underlyingType = Helpers.GetUnderlyingType(type);
			if (underlyingType != null)
			{
				type = underlyingType;
			}
			flag = ValueMember.TryGetCoreSerializer(this, DataFormat.Default, type, out var _, asReference: false, dynamicType: false, overwriteList: false, allowComplexTypes: false) != null;
			if (!flag)
			{
				int num = FindOrAddAuto(type, demand: false, addWithContractOnly: false);
				if (num < 0)
				{
					throw new ArgumentException("The type specified is not a contract-type", "type");
				}
				metaType = ((MetaType)types[num]).GetSurrogateOrBaseOrSelf(deep: false);
				basicList.Add(metaType);
				CascadeDependents(basicList, metaType);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = null;
		if (!flag)
		{
			foreach (MetaType item in (IEnumerable)((metaType == null) ? types : basicList))
			{
				if (item.IsList)
				{
					continue;
				}
				string @namespace = item.Type.Namespace;
				if (!string.IsNullOrEmpty(@namespace) && !@namespace.StartsWith("System."))
				{
					if (text == null)
					{
						text = @namespace;
					}
					else if (!(text == @namespace))
					{
						text = null;
						break;
					}
				}
			}
		}
		switch (syntax)
		{
		case ProtoSyntax.Proto2:
			stringBuilder.AppendLine("syntax = \"proto2\";");
			break;
		case ProtoSyntax.Proto3:
			stringBuilder.AppendLine("syntax = \"proto3\";");
			break;
		default:
			throw new ArgumentOutOfRangeException("syntax");
		}
		if (!string.IsNullOrEmpty(text))
		{
			stringBuilder.Append("package ").Append(text).Append(';');
			Helpers.AppendLine(stringBuilder);
		}
		CommonImports imports = CommonImports.None;
		StringBuilder stringBuilder2 = new StringBuilder();
		MetaType[] array = new MetaType[basicList.Count];
		basicList.CopyTo(array, 0);
		Array.Sort(array, MetaType.Comparer.Default);
		if (flag)
		{
			Helpers.AppendLine(stringBuilder2).Append("message ").Append(type.Name)
				.Append(" {");
			MetaType.NewLine(stringBuilder2, 1).Append((syntax == ProtoSyntax.Proto2) ? "optional " : "").Append(GetSchemaTypeName(type, DataFormat.Default, asReference: false, dynamicType: false, ref imports))
				.Append(" value = 1;");
			Helpers.AppendLine(stringBuilder2).Append('}');
		}
		else
		{
			foreach (MetaType metaType3 in array)
			{
				if (!metaType3.IsList || metaType3 == metaType)
				{
					metaType3.WriteSchema(stringBuilder2, 0, ref imports, syntax);
				}
			}
		}
		if ((imports & CommonImports.Bcl) != 0)
		{
			stringBuilder.Append("import \"protobuf-net/bcl.proto\"; // schema for protobuf-net's handling of core .NET types");
			Helpers.AppendLine(stringBuilder);
		}
		if ((imports & CommonImports.Protogen) != 0)
		{
			stringBuilder.Append("import \"protobuf-net/protogen.proto\"; // custom protobuf-net options");
			Helpers.AppendLine(stringBuilder);
		}
		if ((imports & CommonImports.Timestamp) != 0)
		{
			stringBuilder.Append("import \"google/protobuf/timestamp.proto\";");
			Helpers.AppendLine(stringBuilder);
		}
		if ((imports & CommonImports.Duration) != 0)
		{
			stringBuilder.Append("import \"google/protobuf/duration.proto\";");
			Helpers.AppendLine(stringBuilder);
		}
		return Helpers.AppendLine(stringBuilder.Append(stringBuilder2)).ToString();
	}

	private void CascadeDependents(BasicList list, MetaType metaType)
	{
		if (metaType.IsList)
		{
			Type listItemType = TypeModel.GetListItemType(this, metaType.Type);
			TryGetCoreSerializer(list, listItemType);
			return;
		}
		if (metaType.IsAutoTuple)
		{
			if (MetaType.ResolveTupleConstructor(metaType.Type, out MemberInfo[] mappedMembers) != null)
			{
				for (int i = 0; i < mappedMembers.Length; i++)
				{
					Type itemType = null;
					if (mappedMembers[i] is PropertyInfo)
					{
						itemType = ((PropertyInfo)mappedMembers[i]).PropertyType;
					}
					else if (mappedMembers[i] is FieldInfo)
					{
						itemType = ((FieldInfo)mappedMembers[i]).FieldType;
					}
					TryGetCoreSerializer(list, itemType);
				}
			}
		}
		else
		{
			foreach (ValueMember field in metaType.Fields)
			{
				Type valueType = field.ItemType;
				if (field.IsMap)
				{
					field.ResolveMapTypes(out Type _, out Type _, out valueType);
				}
				if (valueType == null)
				{
					valueType = field.MemberType;
				}
				TryGetCoreSerializer(list, valueType);
			}
		}
		foreach (Type allGenericArgument in metaType.GetAllGenericArguments())
		{
			TryGetCoreSerializer(list, allGenericArgument);
		}
		MetaType surrogateOrSelf;
		if (metaType.HasSubtypes)
		{
			SubType[] subtypes = metaType.GetSubtypes();
			for (int j = 0; j < subtypes.Length; j++)
			{
				surrogateOrSelf = subtypes[j].DerivedType.GetSurrogateOrSelf();
				if (!list.Contains(surrogateOrSelf))
				{
					list.Add(surrogateOrSelf);
					CascadeDependents(list, surrogateOrSelf);
				}
			}
		}
		surrogateOrSelf = metaType.BaseType;
		if (surrogateOrSelf != null)
		{
			surrogateOrSelf = surrogateOrSelf.GetSurrogateOrSelf();
		}
		if (surrogateOrSelf != null && !list.Contains(surrogateOrSelf))
		{
			list.Add(surrogateOrSelf);
			CascadeDependents(list, surrogateOrSelf);
		}
	}

	private void TryGetCoreSerializer(BasicList list, Type itemType)
	{
		if (ValueMember.TryGetCoreSerializer(this, DataFormat.Default, itemType, out var _, asReference: false, dynamicType: false, overwriteList: false, allowComplexTypes: false) != null)
		{
			return;
		}
		int num = FindOrAddAuto(itemType, demand: false, addWithContractOnly: false);
		if (num >= 0)
		{
			MetaType surrogateOrBaseOrSelf = ((MetaType)types[num]).GetSurrogateOrBaseOrSelf(deep: false);
			if (!list.Contains(surrogateOrBaseOrSelf))
			{
				list.Add(surrogateOrBaseOrSelf);
				CascadeDependents(list, surrogateOrBaseOrSelf);
			}
		}
	}

	/// <summary>
	/// Creates a new runtime model, to which the caller
	/// can add support for a range of types. A model
	/// can be used "as is", or can be compiled for
	/// optimal performance.
	/// </summary>
	/// <param name="name">not used currently; this is for compatibility with v3</param>
	public static RuntimeTypeModel Create(string name = null)
	{
		return new RuntimeTypeModel(isDefault: false);
	}

	private RuntimeTypeModel(bool isDefault)
	{
		AutoAddMissingTypes = true;
		UseImplicitZeroDefaults = true;
		SetOption(2, isDefault);
	}

	internal MetaType FindWithoutAdd(Type type)
	{
		BasicList.NodeEnumerator enumerator = types.GetEnumerator();
		while (enumerator.MoveNext())
		{
			MetaType metaType = (MetaType)enumerator.Current;
			if (metaType.Type == type)
			{
				if (metaType.Pending)
				{
					WaitOnLock(metaType);
				}
				return metaType;
			}
		}
		Type type2 = TypeModel.ResolveProxies(type);
		if (!(type2 == null))
		{
			return FindWithoutAdd(type2);
		}
		return null;
	}

	private static bool MetaTypeFinderImpl(object value, object ctx)
	{
		return ((MetaType)value).Type == (Type)ctx;
	}

	private static bool BasicTypeFinderImpl(object value, object ctx)
	{
		return ((BasicType)value).Type == (Type)ctx;
	}

	private void WaitOnLock(MetaType type)
	{
		int opaqueToken = 0;
		try
		{
			TakeLock(ref opaqueToken);
		}
		finally
		{
			ReleaseLock(opaqueToken);
		}
	}

	internal IProtoSerializer TryGetBasicTypeSerializer(Type type)
	{
		int num = basicTypes.IndexOf(BasicTypeFinder, type);
		if (num >= 0)
		{
			return ((BasicType)basicTypes[num]).Serializer;
		}
		lock (basicTypes)
		{
			num = basicTypes.IndexOf(BasicTypeFinder, type);
			if (num >= 0)
			{
				return ((BasicType)basicTypes[num]).Serializer;
			}
			WireType defaultWireType;
			IProtoSerializer protoSerializer = ((MetaType.GetContractFamily(this, type, null) == MetaType.AttributeFamily.None) ? ValueMember.TryGetCoreSerializer(this, DataFormat.Default, type, out defaultWireType, asReference: false, dynamicType: false, overwriteList: false, allowComplexTypes: false) : null);
			if (protoSerializer != null)
			{
				basicTypes.Add(new BasicType(type, protoSerializer));
			}
			return protoSerializer;
		}
	}

	internal int FindOrAddAuto(Type type, bool demand, bool addWithContractOnly, bool addEvenIfAutoDisabled = false)
	{
		int num = types.IndexOf(MetaTypeFinder, type);
		if (num >= 0)
		{
			MetaType metaType = (MetaType)types[num];
			if (metaType.Pending)
			{
				WaitOnLock(metaType);
			}
			return num;
		}
		bool flag = AutoAddMissingTypes || addEvenIfAutoDisabled;
		if (!Helpers.IsEnum(type) && TryGetBasicTypeSerializer(type) != null)
		{
			if (flag && !addWithContractOnly)
			{
				throw MetaType.InbuiltType(type);
			}
			return -1;
		}
		Type type2 = TypeModel.ResolveProxies(type);
		if (type2 != null && type2 != type)
		{
			num = types.IndexOf(MetaTypeFinder, type2);
			type = type2;
		}
		if (num < 0)
		{
			int opaqueToken = 0;
			bool flag2 = false;
			try
			{
				TakeLock(ref opaqueToken);
				MetaType metaType;
				if ((metaType = RecogniseCommonTypes(type)) == null)
				{
					MetaType.AttributeFamily contractFamily = MetaType.GetContractFamily(this, type, null);
					if (contractFamily == MetaType.AttributeFamily.AutoTuple)
					{
						flag = (addEvenIfAutoDisabled = true);
					}
					if (!flag || (!Helpers.IsEnum(type) && addWithContractOnly && contractFamily == MetaType.AttributeFamily.None))
					{
						if (demand)
						{
							TypeModel.ThrowUnexpectedType(type);
						}
						return num;
					}
					metaType = Create(type);
				}
				metaType.Pending = true;
				int num2 = types.IndexOf(MetaTypeFinder, type);
				if (num2 < 0)
				{
					ThrowIfFrozen();
					num = types.Add(metaType);
					flag2 = true;
				}
				else
				{
					num = num2;
				}
				if (flag2)
				{
					metaType.ApplyDefaultBehaviour();
					metaType.Pending = false;
				}
			}
			finally
			{
				ReleaseLock(opaqueToken);
				if (flag2)
				{
					ResetKeyCache();
				}
			}
		}
		return num;
	}

	private MetaType RecogniseCommonTypes(Type type)
	{
		return null;
	}

	private MetaType Create(Type type)
	{
		ThrowIfFrozen();
		return new MetaType(this, type, defaultFactory);
	}

	/// <summary>
	/// Adds support for an additional type in this model, optionally
	/// applying inbuilt patterns. If the type is already known to the
	/// model, the existing type is returned **without** applying
	/// any additional behaviour.
	/// </summary>
	/// <remarks>
	/// Inbuilt patterns include:
	/// [ProtoContract]/[ProtoMember(n)]
	/// [DataContract]/[DataMember(Order=n)]
	/// [XmlType]/[XmlElement(Order=n)]
	/// [On{Des|S}erializ{ing|ed}]
	/// ShouldSerialize*/*Specified
	/// </remarks>
	/// <param name="type">The type to be supported</param>
	/// <param name="applyDefaultBehaviour">
	/// Whether to apply the inbuilt configuration patterns (via attributes etc), or
	/// just add the type with no additional configuration (the type must then be manually configured).
	/// </param>
	/// <returns>
	/// The MetaType representing this type, allowing
	/// further configuration.
	/// </returns>
	public MetaType Add(Type type, bool applyDefaultBehaviour)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		MetaType metaType = FindWithoutAdd(type);
		if (metaType != null)
		{
			return metaType;
		}
		int opaqueToken = 0;
		if (type.IsInterface && MapType(MetaType.ienumerable).IsAssignableFrom(type) && TypeModel.GetListItemType(this, type) == null)
		{
			throw new ArgumentException("IEnumerable[<T>] data cannot be used as a meta-type unless an Add method can be resolved");
		}
		try
		{
			metaType = RecogniseCommonTypes(type);
			if (metaType != null)
			{
				if (!applyDefaultBehaviour)
				{
					throw new ArgumentException("Default behaviour must be observed for certain types with special handling; " + type.FullName, "applyDefaultBehaviour");
				}
				applyDefaultBehaviour = false;
			}
			if (metaType == null)
			{
				metaType = Create(type);
			}
			metaType.Pending = true;
			TakeLock(ref opaqueToken);
			if (FindWithoutAdd(type) != null)
			{
				throw new ArgumentException("Duplicate type", "type");
			}
			ThrowIfFrozen();
			types.Add(metaType);
			if (applyDefaultBehaviour)
			{
				metaType.ApplyDefaultBehaviour();
			}
			metaType.Pending = false;
			return metaType;
		}
		finally
		{
			ReleaseLock(opaqueToken);
			ResetKeyCache();
		}
	}

	/// <summary>
	/// Verifies that the model is still open to changes; if not, an exception is thrown
	/// </summary>
	private void ThrowIfFrozen()
	{
		if (GetOption(4))
		{
			throw new InvalidOperationException("The model cannot be changed once frozen");
		}
	}

	/// <summary>
	/// Prevents further changes to this model
	/// </summary>
	public void Freeze()
	{
		if (GetOption(2))
		{
			throw new InvalidOperationException("The default model cannot be frozen");
		}
		SetOption(4, value: true);
	}

	/// <summary>
	/// Provides the key that represents a given type in the current model.
	/// </summary>
	protected override int GetKeyImpl(Type type)
	{
		return GetKey(type, demand: false, getBaseKey: true);
	}

	internal int GetKey(Type type, bool demand, bool getBaseKey)
	{
		try
		{
			int num = FindOrAddAuto(type, demand, addWithContractOnly: false);
			if (num >= 0)
			{
				MetaType source = (MetaType)types[num];
				if (getBaseKey)
				{
					source = MetaType.GetRootType(source);
					num = FindOrAddAuto(source.Type, demand: true, addWithContractOnly: true);
				}
			}
			return num;
		}
		catch (NotSupportedException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			if (ex2.Message.IndexOf(type.FullName) >= 0)
			{
				throw;
			}
			throw new ProtoException(ex2.Message + " (" + type.FullName + ")", ex2);
		}
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream.
	/// </summary>
	/// <param name="key">Represents the type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="dest">The destination stream to write to.</param>
	protected internal override void Serialize(int key, object value, ProtoWriter dest)
	{
		((MetaType)types[key]).Serializer.Write(value, dest);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="key">Represents the type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	protected internal override object Deserialize(int key, object value, ProtoReader source)
	{
		IProtoSerializer serializer = ((MetaType)types[key]).Serializer;
		if (value == null && Helpers.IsValueType(serializer.ExpectedType))
		{
			if (serializer.RequiresOldValue)
			{
				value = Activator.CreateInstance(serializer.ExpectedType);
			}
			return serializer.Read(value, source);
		}
		return serializer.Read(value, source);
	}

	internal bool IsPrepared(Type type)
	{
		return FindWithoutAdd(type)?.IsPrepared() ?? false;
	}

	internal EnumSerializer.EnumPair[] GetEnumMap(Type type)
	{
		int num = FindOrAddAuto(type, demand: false, addWithContractOnly: false);
		if (num >= 0)
		{
			return ((MetaType)types[num]).GetEnumMap();
		}
		return null;
	}

	internal void TakeLock(ref int opaqueToken)
	{
		opaqueToken = 0;
		if (Monitor.TryEnter(types, metadataTimeoutMilliseconds))
		{
			opaqueToken = GetContention();
			return;
		}
		AddContention();
		throw new TimeoutException("Timeout while inspecting metadata; this may indicate a deadlock. This can often be avoided by preparing necessary serializers during application initialization, rather than allowing multiple threads to perform the initial metadata inspection; please also see the LockContended event");
	}

	private int GetContention()
	{
		return Interlocked.CompareExchange(ref contentionCounter, 0, 0);
	}

	private void AddContention()
	{
		Interlocked.Increment(ref contentionCounter);
	}

	internal void ReleaseLock(int opaqueToken)
	{
		if (opaqueToken == 0)
		{
			return;
		}
		Monitor.Exit(types);
		if (opaqueToken == GetContention())
		{
			return;
		}
		LockContentedEventHandler lockContentedEventHandler = this.LockContended;
		if (lockContentedEventHandler != null)
		{
			string stackTrace;
			try
			{
				throw new ProtoException();
			}
			catch (Exception ex)
			{
				stackTrace = ex.StackTrace;
			}
			lockContentedEventHandler(this, new LockContentedEventArgs(stackTrace));
		}
	}

	internal void ResolveListTypes(Type type, ref Type itemType, ref Type defaultType)
	{
		if (type == null || Helpers.GetTypeCode(type) != ProtoTypeCode.Unknown)
		{
			return;
		}
		if (type.IsArray)
		{
			if (type.GetArrayRank() != 1)
			{
				throw new NotSupportedException("Multi-dimension arrays are supported");
			}
			itemType = type.GetElementType();
			if (itemType == MapType(typeof(byte)))
			{
				defaultType = (itemType = null);
			}
			else
			{
				defaultType = type;
			}
		}
		else if (this[type].IgnoreListHandling)
		{
			return;
		}
		if (itemType == null)
		{
			itemType = TypeModel.GetListItemType(this, type);
		}
		if (itemType != null)
		{
			Type itemType2 = null;
			Type defaultType2 = null;
			ResolveListTypes(itemType, ref itemType2, ref defaultType2);
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
			if (type.IsGenericType && type.GetGenericTypeDefinition() == MapType(typeof(IDictionary<, >)) && itemType == MapType(typeof(KeyValuePair<, >)).MakeGenericType(genericArguments = type.GetGenericArguments()))
			{
				defaultType = MapType(typeof(Dictionary<, >)).MakeGenericType(genericArguments);
			}
			else
			{
				defaultType = MapType(typeof(List<>)).MakeGenericType(itemType);
			}
		}
		if (defaultType != null && !Helpers.IsAssignableFrom(type, defaultType))
		{
			defaultType = null;
		}
	}

	internal string GetSchemaTypeName(Type effectiveType, DataFormat dataFormat, bool asReference, bool dynamicType, ref CommonImports imports)
	{
		Type underlyingType = Helpers.GetUnderlyingType(effectiveType);
		if (underlyingType != null)
		{
			effectiveType = underlyingType;
		}
		if (effectiveType == MapType(typeof(byte[])))
		{
			return "bytes";
		}
		WireType defaultWireType;
		IProtoSerializer protoSerializer = ValueMember.TryGetCoreSerializer(this, dataFormat, effectiveType, out defaultWireType, asReference: false, dynamicType: false, overwriteList: false, allowComplexTypes: false);
		if (protoSerializer == null)
		{
			if (asReference || dynamicType)
			{
				imports |= CommonImports.Bcl;
				return ".bcl.NetObjectProxy";
			}
			return this[effectiveType].GetSurrogateOrBaseOrSelf(deep: true).GetSchemaTypeName();
		}
		if (protoSerializer is ParseableSerializer)
		{
			if (asReference)
			{
				imports |= CommonImports.Bcl;
			}
			if (!asReference)
			{
				return "string";
			}
			return ".bcl.NetObjectProxy";
		}
		switch (Helpers.GetTypeCode(effectiveType))
		{
		case ProtoTypeCode.Boolean:
			return "bool";
		case ProtoTypeCode.Single:
			return "float";
		case ProtoTypeCode.Double:
			return "double";
		case ProtoTypeCode.String:
			if (asReference)
			{
				imports |= CommonImports.Bcl;
			}
			if (!asReference)
			{
				return "string";
			}
			return ".bcl.NetObjectProxy";
		case ProtoTypeCode.Char:
		case ProtoTypeCode.Byte:
		case ProtoTypeCode.UInt16:
		case ProtoTypeCode.UInt32:
			if (dataFormat == DataFormat.FixedSize)
			{
				return "fixed32";
			}
			return "uint32";
		case ProtoTypeCode.SByte:
		case ProtoTypeCode.Int16:
		case ProtoTypeCode.Int32:
			return dataFormat switch
			{
				DataFormat.ZigZag => "sint32", 
				DataFormat.FixedSize => "sfixed32", 
				_ => "int32", 
			};
		case ProtoTypeCode.UInt64:
			if (dataFormat == DataFormat.FixedSize)
			{
				return "fixed64";
			}
			return "uint64";
		case ProtoTypeCode.Int64:
			return dataFormat switch
			{
				DataFormat.ZigZag => "sint64", 
				DataFormat.FixedSize => "sfixed64", 
				_ => "int64", 
			};
		case ProtoTypeCode.DateTime:
			switch (dataFormat)
			{
			case DataFormat.FixedSize:
				return "sint64";
			case DataFormat.WellKnown:
				imports |= CommonImports.Timestamp;
				return ".google.protobuf.Timestamp";
			default:
				imports |= CommonImports.Bcl;
				return ".bcl.DateTime";
			}
		case ProtoTypeCode.TimeSpan:
			switch (dataFormat)
			{
			case DataFormat.FixedSize:
				return "sint64";
			case DataFormat.WellKnown:
				imports |= CommonImports.Duration;
				return ".google.protobuf.Duration";
			default:
				imports |= CommonImports.Bcl;
				return ".bcl.TimeSpan";
			}
		case ProtoTypeCode.Decimal:
			imports |= CommonImports.Bcl;
			return ".bcl.Decimal";
		case ProtoTypeCode.Guid:
			imports |= CommonImports.Bcl;
			return ".bcl.Guid";
		case ProtoTypeCode.Type:
			return "string";
		default:
			throw new NotSupportedException("No .proto map found for: " + effectiveType.FullName);
		}
	}

	/// <summary>
	/// Designate a factory-method to use to create instances of any type; note that this only affect types seen by the serializer *after* setting the factory.
	/// </summary>
	public void SetDefaultFactory(MethodInfo methodInfo)
	{
		VerifyFactory(methodInfo, null);
		defaultFactory = methodInfo;
	}

	internal void VerifyFactory(MethodInfo factory, Type type)
	{
		if (factory != null)
		{
			if (type != null && Helpers.IsValueType(type))
			{
				throw new InvalidOperationException();
			}
			if (!factory.IsStatic)
			{
				throw new ArgumentException("A factory-method must be static", "factory");
			}
			if (type != null && factory.ReturnType != type && factory.ReturnType != MapType(typeof(object)))
			{
				throw new ArgumentException("The factory-method must return object" + ((type == null) ? "" : (" or " + type.FullName)), "factory");
			}
			if (!CallbackSet.CheckCallbackParameters(this, factory))
			{
				throw new ArgumentException("Invalid factory signature in " + factory.DeclaringType.FullName + "." + factory.Name, "factory");
			}
		}
	}

	internal static void OnBeforeApplyDefaultBehaviour(MetaType metaType, ref TypeAddedEventArgs args)
	{
		OnApplyDefaultBehaviour((metaType?.Model as RuntimeTypeModel)?.BeforeApplyDefaultBehaviour, metaType, ref args);
	}

	internal static void OnAfterApplyDefaultBehaviour(MetaType metaType, ref TypeAddedEventArgs args)
	{
		OnApplyDefaultBehaviour((metaType?.Model as RuntimeTypeModel)?.AfterApplyDefaultBehaviour, metaType, ref args);
	}

	private static void OnApplyDefaultBehaviour(EventHandler<TypeAddedEventArgs> handler, MetaType metaType, ref TypeAddedEventArgs args)
	{
		if (handler != null)
		{
			if (args == null)
			{
				args = new TypeAddedEventArgs(metaType);
			}
			handler(metaType.Model, args);
		}
	}
}
