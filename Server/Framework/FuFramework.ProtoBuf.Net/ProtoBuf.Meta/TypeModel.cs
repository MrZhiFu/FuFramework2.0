using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Meta;

/// <summary>
/// Provides protobuf serialization support for a number of types
/// </summary>
public abstract class TypeModel : IProtoInput<Stream>, IProtoInput<ArraySegment<byte>>, IProtoInput<byte[]>, IProtoOutput<Stream>
{
	private sealed class DeserializeItemsIterator<T> : DeserializeItemsIterator, IEnumerator<T>, IEnumerator, IDisposable, IEnumerable<T>, IEnumerable
	{
		public new T Current => (T)base.Current;

		public DeserializeItemsIterator(TypeModel model, Stream source, PrefixStyle style, int expectedField, SerializationContext context)
			: base(model, source, model.MapType(typeof(T)), style, expectedField, null, context)
		{
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return this;
		}

		void IDisposable.Dispose()
		{
		}
	}

	private class DeserializeItemsIterator : IEnumerator, IEnumerable
	{
		private readonly SerializationContext context;

		private readonly int expectedField;

		private readonly TypeModel model;

		private readonly Serializer.TypeResolver resolver;

		private readonly Stream source;

		private readonly PrefixStyle style;

		private readonly Type type;

		private bool haveObject;

		public object Current { get; private set; }

		public DeserializeItemsIterator(TypeModel model, Stream source, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver, SerializationContext context)
		{
			haveObject = true;
			this.source = source;
			this.type = type;
			this.style = style;
			this.expectedField = expectedField;
			this.resolver = resolver;
			this.model = model;
			this.context = context;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}

		public bool MoveNext()
		{
			if (haveObject)
			{
				Current = model.DeserializeWithLengthPrefix(source, null, type, style, expectedField, resolver, out var _, out haveObject, context);
			}
			return haveObject;
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private readonly struct KnownTypeKey
	{
		public int Key { get; }

		public Type Type { get; }

		public KnownTypeKey(Type type, int key)
		{
			Type = type;
			Key = key;
		}
	}

	/// <summary>
	/// Indicates the type of callback to be used
	/// </summary>
	protected internal enum CallbackType
	{
		/// <summary>
		/// Invoked before an object is serialized
		/// </summary>
		BeforeSerialize,
		/// <summary>
		/// Invoked after an object is serialized
		/// </summary>
		AfterSerialize,
		/// <summary>
		/// Invoked before an object is deserialized (or when a new instance is created)
		/// </summary>
		BeforeDeserialize,
		/// <summary>
		/// Invoked after an object is deserialized
		/// </summary>
		AfterDeserialize
	}

	private static readonly Type ilist = typeof(IList);

	private readonly Dictionary<Type, KnownTypeKey> knownKeys = new Dictionary<Type, KnownTypeKey>();

	/// <summary>
	/// Used to provide custom services for writing and parsing type names when using dynamic types. Both parsing and formatting
	/// are provided on a single API as it is essential that both are mapped identically at all times.
	/// </summary>
	public event TypeFormatEventHandler DynamicTypeFormatting;

	/// <summary>
	/// Should the <c>Kind</c> be included on date/time values?
	/// </summary>
	protected internal virtual bool SerializeDateTimeKind()
	{
		return false;
	}

	/// <summary>
	/// Resolve a System.Type to the compiler-specific type
	/// </summary>
	protected internal Type MapType(Type type)
	{
		return MapType(type, demand: true);
	}

	/// <summary>
	/// Resolve a System.Type to the compiler-specific type
	/// </summary>
	protected internal virtual Type MapType(Type type, bool demand)
	{
		return type;
	}

	private WireType GetWireType(ProtoTypeCode code, DataFormat format, ref Type type, out int modelKey)
	{
		modelKey = -1;
		if (Helpers.IsEnum(type))
		{
			modelKey = GetKey(ref type);
			return WireType.Variant;
		}
		switch (code)
		{
		case ProtoTypeCode.Int64:
		case ProtoTypeCode.UInt64:
			if (format != DataFormat.FixedSize)
			{
				return WireType.Variant;
			}
			return WireType.Fixed64;
		case ProtoTypeCode.Boolean:
		case ProtoTypeCode.Char:
		case ProtoTypeCode.SByte:
		case ProtoTypeCode.Byte:
		case ProtoTypeCode.Int16:
		case ProtoTypeCode.UInt16:
		case ProtoTypeCode.Int32:
		case ProtoTypeCode.UInt32:
			if (format != DataFormat.FixedSize)
			{
				return WireType.Variant;
			}
			return WireType.Fixed32;
		case ProtoTypeCode.Double:
			return WireType.Fixed64;
		case ProtoTypeCode.Single:
			return WireType.Fixed32;
		case ProtoTypeCode.Decimal:
		case ProtoTypeCode.DateTime:
		case ProtoTypeCode.String:
		case ProtoTypeCode.TimeSpan:
		case ProtoTypeCode.ByteArray:
		case ProtoTypeCode.Guid:
		case ProtoTypeCode.Uri:
			return WireType.String;
		default:
			if ((modelKey = GetKey(ref type)) >= 0)
			{
				return WireType.String;
			}
			return WireType.None;
		}
	}

	/// <summary>
	/// This is the more "complete" version of Serialize, which handles single instances of mapped types.
	/// The value is written as a complete field, including field-header and (for sub-objects) a
	/// length-prefix
	/// In addition to that, this provides support for:
	/// - basic values; individual int / string / Guid / etc
	/// - IEnumerable sequences of any type handled by TrySerializeAuxiliaryType
	/// </summary>
	internal bool TrySerializeAuxiliaryType(ProtoWriter writer, Type type, DataFormat format, int tag, object value, bool isInsideList, object parentList)
	{
		if (type == null)
		{
			type = value.GetType();
		}
		ProtoTypeCode typeCode = Helpers.GetTypeCode(type);
		int modelKey;
		WireType wireType = GetWireType(typeCode, format, ref type, out modelKey);
		if (modelKey >= 0)
		{
			if (Helpers.IsEnum(type))
			{
				Serialize(modelKey, value, writer);
				return true;
			}
			ProtoWriter.WriteFieldHeader(tag, wireType, writer);
			switch (wireType)
			{
			case WireType.None:
				throw ProtoWriter.CreateException(writer);
			case WireType.String:
			case WireType.StartGroup:
			{
				SubItemToken token = ProtoWriter.StartSubItem(value, writer);
				Serialize(modelKey, value, writer);
				ProtoWriter.EndSubItem(token, writer);
				return true;
			}
			default:
				Serialize(modelKey, value, writer);
				return true;
			}
		}
		if (wireType != WireType.None)
		{
			ProtoWriter.WriteFieldHeader(tag, wireType, writer);
		}
		switch (typeCode)
		{
		case ProtoTypeCode.Int16:
			ProtoWriter.WriteInt16((short)value, writer);
			return true;
		case ProtoTypeCode.Int32:
			ProtoWriter.WriteInt32((int)value, writer);
			return true;
		case ProtoTypeCode.Int64:
			ProtoWriter.WriteInt64((long)value, writer);
			return true;
		case ProtoTypeCode.UInt16:
			ProtoWriter.WriteUInt16((ushort)value, writer);
			return true;
		case ProtoTypeCode.UInt32:
			ProtoWriter.WriteUInt32((uint)value, writer);
			return true;
		case ProtoTypeCode.UInt64:
			ProtoWriter.WriteUInt64((ulong)value, writer);
			return true;
		case ProtoTypeCode.Boolean:
			ProtoWriter.WriteBoolean((bool)value, writer);
			return true;
		case ProtoTypeCode.SByte:
			ProtoWriter.WriteSByte((sbyte)value, writer);
			return true;
		case ProtoTypeCode.Byte:
			ProtoWriter.WriteByte((byte)value, writer);
			return true;
		case ProtoTypeCode.Char:
			ProtoWriter.WriteUInt16((char)value, writer);
			return true;
		case ProtoTypeCode.Double:
			ProtoWriter.WriteDouble((double)value, writer);
			return true;
		case ProtoTypeCode.Single:
			ProtoWriter.WriteSingle((float)value, writer);
			return true;
		case ProtoTypeCode.DateTime:
			if (SerializeDateTimeKind())
			{
				BclHelpers.WriteDateTimeWithKind((DateTime)value, writer);
			}
			else
			{
				BclHelpers.WriteDateTime((DateTime)value, writer);
			}
			return true;
		case ProtoTypeCode.Decimal:
			BclHelpers.WriteDecimal((decimal)value, writer);
			return true;
		case ProtoTypeCode.String:
			ProtoWriter.WriteString((string)value, writer);
			return true;
		case ProtoTypeCode.ByteArray:
			ProtoWriter.WriteBytes((byte[])value, writer);
			return true;
		case ProtoTypeCode.TimeSpan:
			BclHelpers.WriteTimeSpan((TimeSpan)value, writer);
			return true;
		case ProtoTypeCode.Guid:
			BclHelpers.WriteGuid((Guid)value, writer);
			return true;
		case ProtoTypeCode.Uri:
			ProtoWriter.WriteString(((Uri)value).OriginalString, writer);
			return true;
		default:
			if (value is IEnumerable enumerable)
			{
				if (isInsideList)
				{
					throw CreateNestedListsNotSupported(parentList?.GetType());
				}
				foreach (object item in enumerable)
				{
					if (item == null)
					{
						throw new NullReferenceException();
					}
					if (!TrySerializeAuxiliaryType(writer, null, format, tag, item, isInsideList: true, enumerable))
					{
						ThrowUnexpectedType(item.GetType());
					}
				}
				return true;
			}
			return false;
		}
	}

	private void SerializeCore(ProtoWriter writer, object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		int key = GetKey(ref type);
		if (key >= 0)
		{
			Serialize(key, value, writer);
		}
		else if (!TrySerializeAuxiliaryType(writer, type, DataFormat.Default, 1, value, isInsideList: false, null))
		{
			ThrowUnexpectedType(type);
		}
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream.
	/// </summary>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="dest">The destination stream to write to.</param>
	public void Serialize(Stream dest, object value)
	{
		Serialize(dest, value, null);
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream.
	/// </summary>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="dest">The destination stream to write to.</param>
	/// <param name="context">Additional information about this serialization operation.</param>
	public void Serialize(Stream dest, object value, SerializationContext context)
	{
		using ProtoWriter protoWriter = ProtoWriter.Create(dest, this, context);
		protoWriter.SetRootObject(value);
		SerializeCore(protoWriter, value);
		protoWriter.Close();
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied writer.
	/// </summary>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="dest">The destination writer to write to.</param>
	public void Serialize(ProtoWriter dest, object value)
	{
		if (dest == null)
		{
			throw new ArgumentNullException("dest");
		}
		dest.CheckDepthFlushlock();
		dest.SetRootObject(value);
		SerializeCore(dest, value);
		dest.CheckDepthFlushlock();
		ProtoWriter.Flush(dest);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
	/// data - useful with network IO.
	/// </summary>
	/// <param name="type">The type being merged.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int fieldNumber)
	{
		long bytesRead;
		return DeserializeWithLengthPrefix(source, value, type, style, fieldNumber, (Serializer.TypeResolver)null, out bytesRead);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
	/// data - useful with network IO.
	/// </summary>
	/// <param name="type">The type being merged.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	/// <param name="resolver">Used to resolve types on a per-field basis.</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver)
	{
		long bytesRead;
		return DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out bytesRead);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
	/// data - useful with network IO.
	/// </summary>
	/// <param name="type">The type being merged.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	/// <param name="resolver">Used to resolve types on a per-field basis.</param>
	/// <param name="bytesRead">Returns the number of bytes consumed by this operation (includes length-prefix overheads and any skipped data).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver, out int bytesRead)
	{
		long bytesRead2;
		bool haveObject;
		object result = DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out bytesRead2, out haveObject, null);
		bytesRead = checked((int)bytesRead2);
		return result;
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
	/// data - useful with network IO.
	/// </summary>
	/// <param name="type">The type being merged.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	/// <param name="resolver">Used to resolve types on a per-field basis.</param>
	/// <param name="bytesRead">Returns the number of bytes consumed by this operation (includes length-prefix overheads and any skipped data).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver, out long bytesRead)
	{
		bool haveObject;
		return DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out bytesRead, out haveObject, null);
	}

	private object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver, out long bytesRead, out bool haveObject, SerializationContext context)
	{
		haveObject = false;
		bytesRead = 0L;
		if (type == null && (style != PrefixStyle.Base128 || resolver == null))
		{
			throw new InvalidOperationException("A type must be provided unless base-128 prefixing is being used in combination with a resolver");
		}
		long num;
		bool flag2;
		do
		{
			bool flag = expectedField > 0 || resolver != null;
			num = ProtoReader.ReadLongLengthPrefix(source, flag, style, out var fieldNumber, out var bytesRead2);
			if (bytesRead2 == 0)
			{
				return value;
			}
			bytesRead += bytesRead2;
			if (num < 0)
			{
				return value;
			}
			if (style == PrefixStyle.Base128)
			{
				if (flag && expectedField == 0 && type == null && resolver != null)
				{
					type = resolver(fieldNumber);
					flag2 = type == null;
				}
				else
				{
					flag2 = expectedField != fieldNumber;
				}
			}
			else
			{
				flag2 = false;
			}
			if (flag2)
			{
				if (num == long.MaxValue)
				{
					throw new InvalidOperationException();
				}
				ProtoReader.Seek(source, num, null);
				bytesRead += num;
			}
		}
		while (flag2);
		ProtoReader protoReader = null;
		try
		{
			protoReader = ProtoReader.Create(source, this, context, num);
			int key = GetKey(ref type);
			if (key >= 0 && !Helpers.IsEnum(type))
			{
				value = Deserialize(key, value, protoReader);
			}
			else if (!TryDeserializeAuxiliaryType(protoReader, DataFormat.Default, 1, type, ref value, skipOtherFields: true, asListItem: false, autoCreate: true, insideList: false, null) && num != 0L)
			{
				ThrowUnexpectedType(type);
			}
			bytesRead += protoReader.LongPosition;
			haveObject = true;
			return value;
		}
		finally
		{
			ProtoReader.Recycle(protoReader);
		}
	}

	/// <summary>
	/// Reads a sequence of consecutive length-prefixed items from a stream, using
	/// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
	/// are directly comparable to serializing multiple items in succession
	/// (use the <see cref="F:ProtoBuf.Serializer.ListItemTag" /> tag to emulate the implicit behavior
	/// when serializing a list/array). When a tag is
	/// specified, any records with different tags are silently omitted. The
	/// tag is ignored. The tag is ignores for fixed-length prefixes.
	/// </summary>
	/// <param name="source">The binary stream containing the serialized records.</param>
	/// <param name="style">The prefix style used in the data.</param>
	/// <param name="expectedField">
	/// The tag of records to return (if non-positive, then no tag is
	/// expected and all records are returned).
	/// </param>
	/// <param name="resolver">On a field-by-field basis, the type of object to deserialize (can be null if "type" is specified). </param>
	/// <param name="type">The type of object to deserialize (can be null if "resolver" is specified).</param>
	/// <returns>The sequence of deserialized objects.</returns>
	public IEnumerable DeserializeItems(Stream source, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver)
	{
		return DeserializeItems(source, type, style, expectedField, resolver, null);
	}

	/// <summary>
	/// Reads a sequence of consecutive length-prefixed items from a stream, using
	/// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
	/// are directly comparable to serializing multiple items in succession
	/// (use the <see cref="F:ProtoBuf.Serializer.ListItemTag" /> tag to emulate the implicit behavior
	/// when serializing a list/array). When a tag is
	/// specified, any records with different tags are silently omitted. The
	/// tag is ignored. The tag is ignores for fixed-length prefixes.
	/// </summary>
	/// <param name="source">The binary stream containing the serialized records.</param>
	/// <param name="style">The prefix style used in the data.</param>
	/// <param name="expectedField">
	/// The tag of records to return (if non-positive, then no tag is
	/// expected and all records are returned).
	/// </param>
	/// <param name="resolver">On a field-by-field basis, the type of object to deserialize (can be null if "type" is specified). </param>
	/// <param name="type">The type of object to deserialize (can be null if "resolver" is specified).</param>
	/// <returns>The sequence of deserialized objects.</returns>
	/// <param name="context">Additional information about this serialization operation.</param>
	public IEnumerable DeserializeItems(Stream source, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver, SerializationContext context)
	{
		return new DeserializeItemsIterator(this, source, type, style, expectedField, resolver, context);
	}

	/// <summary>
	/// Reads a sequence of consecutive length-prefixed items from a stream, using
	/// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
	/// are directly comparable to serializing multiple items in succession
	/// (use the <see cref="F:ProtoBuf.Serializer.ListItemTag" /> tag to emulate the implicit behavior
	/// when serializing a list/array). When a tag is
	/// specified, any records with different tags are silently omitted. The
	/// tag is ignored. The tag is ignores for fixed-length prefixes.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize.</typeparam>
	/// <param name="source">The binary stream containing the serialized records.</param>
	/// <param name="style">The prefix style used in the data.</param>
	/// <param name="expectedField">
	/// The tag of records to return (if non-positive, then no tag is
	/// expected and all records are returned).
	/// </param>
	/// <returns>The sequence of deserialized objects.</returns>
	public IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int expectedField)
	{
		return DeserializeItems<T>(source, style, expectedField, null);
	}

	/// <summary>
	/// Reads a sequence of consecutive length-prefixed items from a stream, using
	/// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
	/// are directly comparable to serializing multiple items in succession
	/// (use the <see cref="F:ProtoBuf.Serializer.ListItemTag" /> tag to emulate the implicit behavior
	/// when serializing a list/array). When a tag is
	/// specified, any records with different tags are silently omitted. The
	/// tag is ignored. The tag is ignores for fixed-length prefixes.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize.</typeparam>
	/// <param name="source">The binary stream containing the serialized records.</param>
	/// <param name="style">The prefix style used in the data.</param>
	/// <param name="expectedField">
	/// The tag of records to return (if non-positive, then no tag is
	/// expected and all records are returned).
	/// </param>
	/// <returns>The sequence of deserialized objects.</returns>
	/// <param name="context">Additional information about this serialization operation.</param>
	public IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int expectedField, SerializationContext context)
	{
		return new DeserializeItemsIterator<T>(this, source, style, expectedField, context);
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream,
	/// with a length-prefix. This is useful for socket programming,
	/// as DeserializeWithLengthPrefix can be used to read the single object back
	/// from an ongoing stream.
	/// </summary>
	/// <param name="type">The type being serialized.</param>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="dest">The destination stream to write to.</param>
	/// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber)
	{
		SerializeWithLengthPrefix(dest, value, type, style, fieldNumber, null);
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream,
	/// with a length-prefix. This is useful for socket programming,
	/// as DeserializeWithLengthPrefix can be used to read the single object back
	/// from an ongoing stream.
	/// </summary>
	/// <param name="type">The type being serialized.</param>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="dest">The destination stream to write to.</param>
	/// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	/// <param name="context">Additional information about this serialization operation.</param>
	public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber, SerializationContext context)
	{
		if (type == null)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			type = MapType(value.GetType());
		}
		int key = GetKey(ref type);
		using ProtoWriter protoWriter = ProtoWriter.Create(dest, this, context);
		switch (style)
		{
		case PrefixStyle.None:
			Serialize(key, value, protoWriter);
			break;
		case PrefixStyle.Base128:
		case PrefixStyle.Fixed32:
		case PrefixStyle.Fixed32BigEndian:
			ProtoWriter.WriteObject(value, key, protoWriter, style, fieldNumber);
			break;
		default:
			throw new ArgumentOutOfRangeException("style");
		}
		protoWriter.Close();
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object Deserialize(Stream source, object value, Type type)
	{
		return Deserialize(source, value, type, null);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	/// <param name="context">Additional information about this serialization operation.</param>
	public object Deserialize(Stream source, object value, Type type, SerializationContext context)
	{
		bool noAutoCreate = PrepareDeserialize(value, ref type);
		ProtoReader protoReader = null;
		try
		{
			protoReader = ProtoReader.Create(source, this, context, -1L);
			if (value != null)
			{
				protoReader.SetRootObject(value);
			}
			object result = DeserializeCore(protoReader, type, value, noAutoCreate);
			protoReader.CheckFullyConsumed();
			return result;
		}
		finally
		{
			ProtoReader.Recycle(protoReader);
		}
	}

	private bool PrepareDeserialize(object value, ref Type type)
	{
		if (type == null)
		{
			if (value == null)
			{
				throw new ArgumentNullException("type");
			}
			type = MapType(value.GetType());
		}
		bool result = true;
		Type underlyingType = Helpers.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			type = underlyingType;
			result = false;
		}
		return result;
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="length">The number of bytes to consume.</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object Deserialize(Stream source, object value, Type type, int length)
	{
		return Deserialize(source, value, type, length, null);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="length">The number of bytes to consume.</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object Deserialize(Stream source, object value, Type type, long length)
	{
		return Deserialize(source, value, type, length, null);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="length">The number of bytes to consume (or -1 to read to the end of the stream).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	/// <param name="context">Additional information about this serialization operation.</param>
	public object Deserialize(Stream source, object value, Type type, int length, SerializationContext context)
	{
		return Deserialize(source, value, type, (length == int.MaxValue) ? long.MaxValue : length, context);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="length">The number of bytes to consume (or -1 to read to the end of the stream).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	/// <param name="context">Additional information about this serialization operation.</param>
	public object Deserialize(Stream source, object value, Type type, long length, SerializationContext context)
	{
		bool noAutoCreate = PrepareDeserialize(value, ref type);
		ProtoReader protoReader = null;
		try
		{
			protoReader = ProtoReader.Create(source, this, context, length);
			if (value != null)
			{
				protoReader.SetRootObject(value);
			}
			object result = DeserializeCore(protoReader, type, value, noAutoCreate);
			protoReader.CheckFullyConsumed();
			return result;
		}
		finally
		{
			ProtoReader.Recycle(protoReader);
		}
	}

	/// <summary>
	/// Applies a protocol-buffer reader to an existing instance (which may be null).
	/// </summary>
	/// <param name="type">The type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be modified (can be null).</param>
	/// <param name="source">The reader to apply to the instance (cannot be null).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public object Deserialize(ProtoReader source, object value, Type type)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		bool noAutoCreate = PrepareDeserialize(value, ref type);
		if (value != null)
		{
			source.SetRootObject(value);
		}
		object result = DeserializeCore(source, type, value, noAutoCreate);
		source.CheckFullyConsumed();
		return result;
	}

	private object DeserializeCore(ProtoReader reader, Type type, object value, bool noAutoCreate)
	{
		int key = GetKey(ref type);
		if (key >= 0 && !Helpers.IsEnum(type))
		{
			return Deserialize(key, value, reader);
		}
		TryDeserializeAuxiliaryType(reader, DataFormat.Default, 1, type, ref value, skipOtherFields: true, asListItem: false, noAutoCreate, insideList: false, null);
		return value;
	}

	internal static MethodInfo ResolveListAdd(TypeModel model, Type listType, Type itemType, out bool isList)
	{
		isList = model.MapType(ilist).IsAssignableFrom(listType);
		Type[] array = new Type[1] { itemType };
		MethodInfo instanceMethod = Helpers.GetInstanceMethod(listType, "Add", array);
		if (instanceMethod == null)
		{
			bool num = listType.IsInterface && model.MapType(typeof(IEnumerable<>)).MakeGenericType(array).IsAssignableFrom(listType);
			Type type = model.MapType(typeof(ICollection<>)).MakeGenericType(array);
			if (num || type.IsAssignableFrom(listType))
			{
				instanceMethod = Helpers.GetInstanceMethod(type, "Add", array);
			}
		}
		if (instanceMethod == null)
		{
			Type[] interfaces = listType.GetInterfaces();
			foreach (Type type2 in interfaces)
			{
				if (type2.Name == "IProducerConsumerCollection`1" && type2.IsGenericType && type2.GetGenericTypeDefinition().FullName == "System.Collections.Concurrent.IProducerConsumerCollection`1")
				{
					instanceMethod = Helpers.GetInstanceMethod(type2, "TryAdd", array);
					if (instanceMethod != null)
					{
						break;
					}
				}
			}
		}
		if (instanceMethod == null)
		{
			array[0] = model.MapType(typeof(object));
			instanceMethod = Helpers.GetInstanceMethod(listType, "Add", array);
		}
		if ((instanceMethod == null) & isList)
		{
			instanceMethod = Helpers.GetInstanceMethod(model.MapType(ilist), "Add", array);
		}
		return instanceMethod;
	}

	internal static Type GetListItemType(TypeModel model, Type listType)
	{
		if (listType == model.MapType(typeof(string)) || listType.IsArray || !model.MapType(typeof(IEnumerable)).IsAssignableFrom(listType))
		{
			return null;
		}
		BasicList basicList = new BasicList();
		MethodInfo[] methods = listType.GetMethods();
		foreach (MethodInfo methodInfo in methods)
		{
			if (!methodInfo.IsStatic && !(methodInfo.Name != "Add"))
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				Type parameterType;
				if (parameters.Length == 1 && !basicList.Contains(parameterType = parameters[0].ParameterType))
				{
					basicList.Add(parameterType);
				}
			}
		}
		string name = listType.Name;
		if (name == null || (name.IndexOf("Queue") < 0 && name.IndexOf("Stack") < 0))
		{
			TestEnumerableListPatterns(model, basicList, listType);
			Type[] interfaces = listType.GetInterfaces();
			foreach (Type iType in interfaces)
			{
				TestEnumerableListPatterns(model, basicList, iType);
			}
		}
		PropertyInfo[] properties = listType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!(propertyInfo.Name != "Item") && !basicList.Contains(propertyInfo.PropertyType))
			{
				ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
				if (indexParameters.Length == 1 && !(indexParameters[0].ParameterType != model.MapType(typeof(int))))
				{
					basicList.Add(propertyInfo.PropertyType);
				}
			}
		}
		switch (basicList.Count)
		{
		case 0:
			return null;
		case 1:
			if ((Type)basicList[0] == listType)
			{
				return null;
			}
			return (Type)basicList[0];
		case 2:
			if ((Type)basicList[0] != listType && CheckDictionaryAccessors(model, (Type)basicList[0], (Type)basicList[1]))
			{
				return (Type)basicList[0];
			}
			if ((Type)basicList[1] != listType && CheckDictionaryAccessors(model, (Type)basicList[1], (Type)basicList[0]))
			{
				return (Type)basicList[1];
			}
			break;
		}
		return null;
	}

	private static void TestEnumerableListPatterns(TypeModel model, BasicList candidates, Type iType)
	{
		if (!iType.IsGenericType)
		{
			return;
		}
		Type genericTypeDefinition = iType.GetGenericTypeDefinition();
		if (genericTypeDefinition == model.MapType(typeof(IEnumerable<>)) || genericTypeDefinition == model.MapType(typeof(ICollection<>)) || genericTypeDefinition.FullName == "System.Collections.Concurrent.IProducerConsumerCollection`1")
		{
			Type[] genericArguments = iType.GetGenericArguments();
			if (!candidates.Contains(genericArguments[0]))
			{
				candidates.Add(genericArguments[0]);
			}
		}
	}

	private static bool CheckDictionaryAccessors(TypeModel model, Type pair, Type value)
	{
		if (pair.IsGenericType && pair.GetGenericTypeDefinition() == model.MapType(typeof(KeyValuePair<, >)))
		{
			return pair.GetGenericArguments()[1] == value;
		}
		return false;
	}

	private bool TryDeserializeList(TypeModel model, ProtoReader reader, DataFormat format, int tag, Type listType, Type itemType, ref object value)
	{
		bool isList;
		MethodInfo methodInfo = ResolveListAdd(model, listType, itemType, out isList);
		if (methodInfo == null)
		{
			throw new NotSupportedException("Unknown list variant: " + listType.FullName);
		}
		bool result = false;
		object value2 = null;
		IList list = value as IList;
		object[] array = (isList ? null : new object[1]);
		BasicList basicList = (listType.IsArray ? new BasicList() : null);
		while (TryDeserializeAuxiliaryType(reader, format, tag, itemType, ref value2, skipOtherFields: true, asListItem: true, autoCreate: true, insideList: true, value ?? listType))
		{
			result = true;
			if (value == null && basicList == null)
			{
				value = CreateListInstance(listType, itemType);
				list = value as IList;
			}
			if (list != null)
			{
				list.Add(value2);
			}
			else if (basicList != null)
			{
				basicList.Add(value2);
			}
			else
			{
				array[0] = value2;
				methodInfo.Invoke(value, array);
			}
			value2 = null;
		}
		if (basicList != null)
		{
			if (value != null)
			{
				if (basicList.Count != 0)
				{
					Array array2 = (Array)value;
					Array array3 = Array.CreateInstance(itemType, array2.Length + basicList.Count);
					Array.Copy(array2, array3, array2.Length);
					basicList.CopyTo(array3, array2.Length);
					value = array3;
				}
			}
			else
			{
				Array array3 = Array.CreateInstance(itemType, basicList.Count);
				basicList.CopyTo(array3, 0);
				value = array3;
			}
		}
		return result;
	}

	private static object CreateListInstance(Type listType, Type itemType)
	{
		Type type = listType;
		if (listType.IsArray)
		{
			return Array.CreateInstance(itemType, 0);
		}
		if (!listType.IsClass || listType.IsAbstract || Helpers.GetConstructor(listType, Helpers.EmptyTypes, nonPublic: true) == null)
		{
			bool flag = false;
			string fullName;
			if (listType.IsInterface && (fullName = listType.FullName) != null && fullName.IndexOf("Dictionary") >= 0)
			{
				if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(IDictionary<, >))
				{
					Type[] genericArguments = listType.GetGenericArguments();
					type = typeof(Dictionary<, >).MakeGenericType(genericArguments);
					flag = true;
				}
				if (!flag && listType == typeof(IDictionary))
				{
					type = typeof(Hashtable);
					flag = true;
				}
			}
			if (!flag)
			{
				type = typeof(List<>).MakeGenericType(itemType);
				flag = true;
			}
			if (!flag)
			{
				type = typeof(ArrayList);
				flag = true;
			}
		}
		return Activator.CreateInstance(type);
	}

	/// <summary>
	/// This is the more "complete" version of Deserialize, which handles single instances of mapped types.
	/// The value is read as a complete field, including field-header and (for sub-objects) a
	/// length-prefix..kmc
	/// In addition to that, this provides support for:
	/// - basic values; individual int / string / Guid / etc
	/// - IList sets of any type handled by TryDeserializeAuxiliaryType
	/// </summary>
	internal bool TryDeserializeAuxiliaryType(ProtoReader reader, DataFormat format, int tag, Type type, ref object value, bool skipOtherFields, bool asListItem, bool autoCreate, bool insideList, object parentListOrType)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		Type type2 = null;
		ProtoTypeCode typeCode = Helpers.GetTypeCode(type);
		int modelKey;
		WireType wireType = GetWireType(typeCode, format, ref type, out modelKey);
		bool flag = false;
		if (wireType == WireType.None)
		{
			type2 = GetListItemType(this, type);
			if (type2 == null && type.IsArray && type.GetArrayRank() == 1 && type != typeof(byte[]))
			{
				type2 = type.GetElementType();
			}
			if (type2 != null)
			{
				if (insideList)
				{
					throw CreateNestedListsNotSupported((parentListOrType as Type) ?? parentListOrType?.GetType());
				}
				flag = TryDeserializeList(this, reader, format, tag, type, type2, ref value);
				if (!flag && autoCreate)
				{
					value = CreateListInstance(type, type2);
				}
				return flag;
			}
			ThrowUnexpectedType(type);
		}
		while (!(flag && asListItem))
		{
			int num = reader.ReadFieldHeader();
			if (num <= 0)
			{
				break;
			}
			if (num != tag)
			{
				if (skipOtherFields)
				{
					reader.SkipField();
					continue;
				}
				throw ProtoReader.AddErrorData(new InvalidOperationException("Expected field " + tag + ", but found " + num), reader);
			}
			flag = true;
			reader.Hint(wireType);
			if (modelKey >= 0)
			{
				if ((uint)(wireType - 2) <= 1u)
				{
					SubItemToken token = ProtoReader.StartSubItem(reader);
					value = Deserialize(modelKey, value, reader);
					ProtoReader.EndSubItem(token, reader);
				}
				else
				{
					value = Deserialize(modelKey, value, reader);
				}
				continue;
			}
			switch (typeCode)
			{
			case ProtoTypeCode.Int16:
				value = reader.ReadInt16();
				break;
			case ProtoTypeCode.Int32:
				value = reader.ReadInt32();
				break;
			case ProtoTypeCode.Int64:
				value = reader.ReadInt64();
				break;
			case ProtoTypeCode.UInt16:
				value = reader.ReadUInt16();
				break;
			case ProtoTypeCode.UInt32:
				value = reader.ReadUInt32();
				break;
			case ProtoTypeCode.UInt64:
				value = reader.ReadUInt64();
				break;
			case ProtoTypeCode.Boolean:
				value = reader.ReadBoolean();
				break;
			case ProtoTypeCode.SByte:
				value = reader.ReadSByte();
				break;
			case ProtoTypeCode.Byte:
				value = reader.ReadByte();
				break;
			case ProtoTypeCode.Char:
				value = (char)reader.ReadUInt16();
				break;
			case ProtoTypeCode.Double:
				value = reader.ReadDouble();
				break;
			case ProtoTypeCode.Single:
				value = reader.ReadSingle();
				break;
			case ProtoTypeCode.DateTime:
				value = BclHelpers.ReadDateTime(reader);
				break;
			case ProtoTypeCode.Decimal:
				value = BclHelpers.ReadDecimal(reader);
				break;
			case ProtoTypeCode.String:
				value = reader.ReadString();
				break;
			case ProtoTypeCode.ByteArray:
				value = ProtoReader.AppendBytes((byte[])value, reader);
				break;
			case ProtoTypeCode.TimeSpan:
				value = BclHelpers.ReadTimeSpan(reader);
				break;
			case ProtoTypeCode.Guid:
				value = BclHelpers.ReadGuid(reader);
				break;
			case ProtoTypeCode.Uri:
				value = new Uri(reader.ReadString(), UriKind.RelativeOrAbsolute);
				break;
			}
		}
		if (!flag && !asListItem && autoCreate && type != typeof(string))
		{
			value = Activator.CreateInstance(type);
		}
		return flag;
	}

	/// <summary>
	/// Creates a new runtime model, to which the caller
	/// can add support for a range of types. A model
	/// can be used "as is", or can be compiled for
	/// optimal performance.
	/// </summary>
	[Obsolete("Please use RuntimeTypeModel.Create", false)]
	public static RuntimeTypeModel Create()
	{
		return RuntimeTypeModel.Create();
	}

	/// <summary>
	/// Applies common proxy scenarios, resolving the actual type to consider
	/// </summary>
	protected internal static Type ResolveProxies(Type type)
	{
		if (type == null)
		{
			return null;
		}
		if (type.IsGenericParameter)
		{
			return null;
		}
		Type underlyingType = Helpers.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			return underlyingType;
		}
		string fullName = type.FullName;
		if (fullName != null && fullName.StartsWith("System.Data.Entity.DynamicProxies."))
		{
			return type.BaseType;
		}
		Type[] interfaces = type.GetInterfaces();
		for (int i = 0; i < interfaces.Length; i++)
		{
			switch (interfaces[i].FullName)
			{
			case "NHibernate.Proxy.INHibernateProxy":
			case "NHibernate.Proxy.DynamicProxy.IProxy":
			case "NHibernate.Intercept.IFieldInterceptorAccessor":
				return type.BaseType;
			}
		}
		return null;
	}

	/// <summary>
	/// Indicates whether the supplied type is explicitly modelled by the model
	/// </summary>
	public bool IsDefined(Type type)
	{
		return GetKey(ref type) >= 0;
	}

	/// <summary>
	/// Provides the key that represents a given type in the current model.
	/// The type is also normalized for proxies at the same time.
	/// </summary>
	protected internal int GetKey(ref Type type)
	{
		if (type == null)
		{
			return -1;
		}
		lock (knownKeys)
		{
			if (knownKeys.TryGetValue(type, out var value))
			{
				type = value.Type;
				return value.Key;
			}
		}
		int keyImpl = GetKeyImpl(type);
		Type key = type;
		if (keyImpl < 0)
		{
			Type type2 = ResolveProxies(type);
			if (type2 != null && type2 != type)
			{
				type = type2;
				keyImpl = GetKeyImpl(type);
			}
		}
		lock (knownKeys)
		{
			knownKeys[key] = new KnownTypeKey(type, keyImpl);
			return keyImpl;
		}
	}

	/// <summary>
	/// Advertise that a type's key can have changed
	/// </summary>
	internal void ResetKeyCache()
	{
		lock (knownKeys)
		{
			knownKeys.Clear();
		}
	}

	/// <summary>
	/// Provides the key that represents a given type in the current model.
	/// </summary>
	protected abstract int GetKeyImpl(Type type);

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream.
	/// </summary>
	/// <param name="key">Represents the type (including inheritance) to consider.</param>
	/// <param name="value">The existing instance to be serialized (cannot be null).</param>
	/// <param name="dest">The destination stream to write to.</param>
	protected internal abstract void Serialize(int key, object value, ProtoWriter dest);

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
	protected internal abstract object Deserialize(int key, object value, ProtoReader source);

	/// <summary>
	/// Create a deep clone of the supplied instance; any sub-items are also cloned.
	/// </summary>
	public object DeepClone(object value)
	{
		if (value == null)
		{
			return null;
		}
		Type type = value.GetType();
		int key = GetKey(ref type);
		if (key >= 0 && !Helpers.IsEnum(type))
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (ProtoWriter protoWriter = ProtoWriter.Create(memoryStream, this))
				{
					protoWriter.SetRootObject(value);
					Serialize(key, value, protoWriter);
					protoWriter.Close();
				}
				memoryStream.Position = 0L;
				ProtoReader protoReader = null;
				try
				{
					protoReader = ProtoReader.Create(memoryStream, this, null, -1L);
					return Deserialize(key, null, protoReader);
				}
				finally
				{
					ProtoReader.Recycle(protoReader);
				}
			}
		}
		if (type == typeof(byte[]))
		{
			byte[] array = (byte[])value;
			byte[] array2 = new byte[array.Length];
			Buffer.BlockCopy(array, 0, array2, 0, array.Length);
			return array2;
		}
		if (GetWireType(Helpers.GetTypeCode(type), DataFormat.Default, ref type, out var modelKey) != WireType.None && modelKey < 0)
		{
			return value;
		}
		using MemoryStream memoryStream2 = new MemoryStream();
		using (ProtoWriter protoWriter2 = ProtoWriter.Create(memoryStream2, this))
		{
			if (!TrySerializeAuxiliaryType(protoWriter2, type, DataFormat.Default, 1, value, isInsideList: false, null))
			{
				ThrowUnexpectedType(type);
			}
			protoWriter2.Close();
		}
		memoryStream2.Position = 0L;
		ProtoReader reader = null;
		try
		{
			reader = ProtoReader.Create(memoryStream2, this, null, -1L);
			value = null;
			TryDeserializeAuxiliaryType(reader, DataFormat.Default, 1, type, ref value, skipOtherFields: true, asListItem: false, autoCreate: true, insideList: false, null);
			return value;
		}
		finally
		{
			ProtoReader.Recycle(reader);
		}
	}

	/// <summary>
	/// Indicates that while an inheritance tree exists, the exact type encountered was not
	/// specified in that hierarchy and cannot be processed.
	/// </summary>
	protected internal static void ThrowUnexpectedSubtype(Type expected, Type actual)
	{
		if (expected != ResolveProxies(actual))
		{
			throw new InvalidOperationException("Unexpected sub-type: " + actual.FullName);
		}
	}

	/// <summary>
	/// Indicates that the given type was not expected, and cannot be processed.
	/// </summary>
	protected internal static void ThrowUnexpectedType(Type type)
	{
		string text = ((type == null) ? "(unknown)" : type.FullName);
		if (type != null)
		{
			Type baseType = type.BaseType;
			if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name == "GeneratedMessage`2")
			{
				throw new InvalidOperationException("Are you mixing protobuf-net and protobuf-csharp-port? See https://stackoverflow.com/q/11564914/23354; type: " + text);
			}
		}
		throw new InvalidOperationException("Type is not expected, and no contract can be inferred: " + text);
	}

	internal static Exception CreateNestedListsNotSupported(Type type)
	{
		return new NotSupportedException("Nested or jagged lists and arrays are not supported: " + (type?.FullName ?? "(null)"));
	}

	/// <summary>
	/// Indicates that the given type cannot be constructed; it may still be possible to
	/// deserialize into existing instances.
	/// </summary>
	public static void ThrowCannotCreateInstance(Type type)
	{
		throw new ProtoException("No parameterless constructor found for " + (type?.FullName ?? "(null)"));
	}

	internal static string SerializeType(TypeModel model, Type type)
	{
		if (model != null)
		{
			TypeFormatEventHandler typeFormatEventHandler = model.DynamicTypeFormatting;
			if (typeFormatEventHandler != null)
			{
				TypeFormatEventArgs typeFormatEventArgs = new TypeFormatEventArgs(type);
				typeFormatEventHandler(model, typeFormatEventArgs);
				if (!string.IsNullOrEmpty(typeFormatEventArgs.FormattedName))
				{
					return typeFormatEventArgs.FormattedName;
				}
			}
		}
		return type.AssemblyQualifiedName;
	}

	internal static Type DeserializeType(TypeModel model, string value)
	{
		if (model != null)
		{
			TypeFormatEventHandler typeFormatEventHandler = model.DynamicTypeFormatting;
			if (typeFormatEventHandler != null)
			{
				TypeFormatEventArgs typeFormatEventArgs = new TypeFormatEventArgs(value);
				typeFormatEventHandler(model, typeFormatEventArgs);
				if (typeFormatEventArgs.Type != null)
				{
					return typeFormatEventArgs.Type;
				}
			}
		}
		return Type.GetType(value);
	}

	/// <summary>
	/// Returns true if the type supplied is either a recognised contract type,
	/// or a *list* of a recognised contract type.
	/// </summary>
	/// <remarks>
	/// Note that primitives always return false, even though the engine
	/// will, if forced, try to serialize such
	/// </remarks>
	/// <returns>True if this type is recognised as a serializable entity, else false</returns>
	public bool CanSerializeContractType(Type type)
	{
		return CanSerialize(type, allowBasic: false, allowContract: true, allowLists: true);
	}

	/// <summary>
	/// Returns true if the type supplied is a basic type with inbuilt handling,
	/// a recognised contract type, or a *list* of a basic / contract type.
	/// </summary>
	public bool CanSerialize(Type type)
	{
		return CanSerialize(type, allowBasic: true, allowContract: true, allowLists: true);
	}

	/// <summary>
	/// Returns true if the type supplied is a basic type with inbuilt handling,
	/// or a *list* of a basic type with inbuilt handling
	/// </summary>
	public bool CanSerializeBasicType(Type type)
	{
		return CanSerialize(type, allowBasic: true, allowContract: false, allowLists: true);
	}

	private bool CanSerialize(Type type, bool allowBasic, bool allowContract, bool allowLists)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		Type underlyingType = Helpers.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			type = underlyingType;
		}
		ProtoTypeCode typeCode = Helpers.GetTypeCode(type);
		if ((uint)typeCode > 1u)
		{
			return allowBasic;
		}
		if (GetKey(ref type) >= 0)
		{
			return allowContract;
		}
		if (allowLists)
		{
			Type type2 = null;
			if (type.IsArray)
			{
				if (type.GetArrayRank() == 1)
				{
					type2 = type.GetElementType();
				}
			}
			else
			{
				type2 = GetListItemType(this, type);
			}
			if (type2 != null)
			{
				return CanSerialize(type2, allowBasic, allowContract, allowLists: false);
			}
		}
		return false;
	}

	/// <summary>
	/// Suggest a .proto definition for the given type
	/// </summary>
	/// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
	/// <returns>The .proto definition as a string</returns>
	public virtual string GetSchema(Type type)
	{
		return GetSchema(type, ProtoSyntax.Proto2);
	}

	/// <summary>
	/// Suggest a .proto definition for the given type
	/// </summary>
	/// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
	/// <returns>The .proto definition as a string</returns>
	/// <param name="syntax">The .proto syntax to use for the operation</param>
	public virtual string GetSchema(Type type, ProtoSyntax syntax)
	{
		throw new NotSupportedException();
	}

	internal virtual Type GetType(string fullName, Assembly context)
	{
		return ResolveKnownType(fullName, this, context);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Type ResolveKnownType(string name, TypeModel model, Assembly assembly)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		try
		{
			Type type = Type.GetType(name);
			if (type != null)
			{
				return type;
			}
		}
		catch
		{
		}
		try
		{
			int num = name.IndexOf(',');
			string name2 = ((num > 0) ? name.Substring(0, num) : name).Trim();
			if (assembly == null)
			{
				assembly = Assembly.GetCallingAssembly();
			}
			Type type2 = assembly?.GetType(name2);
			if (type2 != null)
			{
				return type2;
			}
		}
		catch
		{
		}
		return null;
	}

	T IProtoInput<ArraySegment<byte>>.Deserialize<T>(ArraySegment<byte> source, T value, object userState)
	{
		using MemoryStream source2 = new MemoryStream(source.Array, source.Offset, source.Count);
		return (T)Deserialize(source2, value, typeof(T), CreateContext(userState));
	}

	T IProtoInput<byte[]>.Deserialize<T>(byte[] source, T value, object userState)
	{
		using MemoryStream source2 = new MemoryStream(source);
		return (T)Deserialize(source2, value, typeof(T), CreateContext(userState));
	}

	T IProtoInput<Stream>.Deserialize<T>(Stream source, T value, object userState)
	{
		return (T)Deserialize(source, value, typeof(T), CreateContext(userState));
	}

	void IProtoOutput<Stream>.Serialize<T>(Stream destination, T value, object userState)
	{
		Serialize(destination, value, CreateContext(userState));
	}

	private static SerializationContext CreateContext(object userState)
	{
		if (userState == null)
		{
			return SerializationContext.Default;
		}
		if (userState is SerializationContext result)
		{
			return result;
		}
		SerializationContext serializationContext = new SerializationContext();
		serializationContext.Context = userState;
		serializationContext.Freeze();
		return serializationContext;
	}
}
