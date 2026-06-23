using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf;

/// <summary>
/// Provides protocol-buffer serialization capability for concrete, attributed types. This
/// is a *default* model, but custom serializer models are also supported.
/// </summary>
/// <remarks>
/// Protocol-buffer serialization is a compact binary format, designed to take
/// advantage of sparse data and knowledge of specific data types; it is also
/// extensible, allowing a type to be deserialized / merged even if some data is
/// not recognised.
/// </remarks>
public static class Serializer
{
	/// <summary>
	/// Provides non-generic access to the default serializer.
	/// </summary>
	public static class NonGeneric
	{
		/// <summary>
		/// Create a deep clone of the supplied instance; any sub-items are also cloned.
		/// </summary>
		public static object DeepClone(object instance)
		{
			if (instance != null)
			{
				return RuntimeTypeModel.Default.DeepClone(instance);
			}
			return null;
		}

		/// <summary>
		/// Writes a protocol-buffer representation of the given instance to the supplied stream.
		/// </summary>
		/// <param name="instance">The existing instance to be serialized (cannot be null).</param>
		/// <param name="dest">The destination stream to write to.</param>
		public static void Serialize(Stream dest, object instance)
		{
			if (instance != null)
			{
				RuntimeTypeModel.Default.Serialize(dest, instance);
			}
		}

		/// <summary>
		/// Creates a new instance from a protocol-buffer stream
		/// </summary>
		/// <param name="type">The type to be created.</param>
		/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
		/// <returns>A new, initialized instance.</returns>
		public static object Deserialize(Type type, Stream source)
		{
			return RuntimeTypeModel.Default.Deserialize(source, null, type);
		}

		/// <summary>Applies a protocol-buffer stream to an existing instance.</summary>
		/// <param name="instance">The existing instance to be modified (cannot be null).</param>
		/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
		/// <returns>The updated instance</returns>
		public static object Merge(Stream source, object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			return RuntimeTypeModel.Default.Deserialize(source, instance, instance.GetType(), null);
		}

		/// <summary>
		/// Writes a protocol-buffer representation of the given instance to the supplied stream,
		/// with a length-prefix. This is useful for socket programming,
		/// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
		/// from an ongoing stream.
		/// </summary>
		/// <param name="instance">The existing instance to be serialized (cannot be null).</param>
		/// <param name="style">How to encode the length prefix.</param>
		/// <param name="destination">The destination stream to write to.</param>
		/// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
		public static void SerializeWithLengthPrefix(Stream destination, object instance, PrefixStyle style, int fieldNumber)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			RuntimeTypeModel @default = RuntimeTypeModel.Default;
			@default.SerializeWithLengthPrefix(destination, instance, @default.MapType(instance.GetType()), style, fieldNumber);
		}

		/// <summary>
		/// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
		/// data - useful with network IO.
		/// </summary>
		/// <param name="value">The existing instance to be modified (can be null).</param>
		/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
		/// <param name="style">How to encode the length prefix.</param>
		/// <param name="resolver">Used to resolve types on a per-field basis.</param>
		/// <returns>
		/// The updated instance; this may be different to the instance argument if
		/// either the original instance was null, or the stream defines a known sub-type of the
		/// original instance.
		/// </returns>
		public static bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style, TypeResolver resolver, out object value)
		{
			value = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(source, null, null, style, 0, resolver);
			return value != null;
		}

		/// <summary>
		/// Indicates whether the supplied type is explicitly modelled by the model
		/// </summary>
		public static bool CanSerialize(Type type)
		{
			return RuntimeTypeModel.Default.IsDefined(type);
		}

		/// <summary>
		/// Precompiles the serializer for a given type.
		/// </summary>
		public static void PrepareSerializer(Type t)
		{
		}
	}

	/// <summary>
	/// Global switches that change the behavior of protobuf-net
	/// </summary>
	public static class GlobalOptions
	{
		/// <summary>
		///     <see cref="P:ProtoBuf.Meta.RuntimeTypeModel.InferTagFromNameDefault" />
		/// </summary>
		[Obsolete("Please use RuntimeTypeModel.Default.InferTagFromNameDefault instead (or on a per-model basis)", false)]
		public static bool InferTagFromName
		{
			get
			{
				return RuntimeTypeModel.Default.InferTagFromNameDefault;
			}
			set
			{
				RuntimeTypeModel.Default.InferTagFromNameDefault = value;
			}
		}
	}

	/// <summary>
	/// Maps a field-number to a type
	/// </summary>
	public delegate Type TypeResolver(int fieldNumber);

	private const string ProtoBinaryField = "proto";

	/// <summary>
	/// The field number that is used as a default when serializing/deserializing a list of objects.
	/// The data is treated as repeated message with field number 1.
	/// </summary>
	public const int ListItemTag = 1;

	/// <summary>
	/// Suggest a .proto definition for the given type
	/// </summary>
	/// <typeparam name="T">The type to generate a .proto definition for</typeparam>
	/// <returns>The .proto definition as a string</returns>
	public static string GetProto<T>()
	{
		return GetProto<T>(ProtoSyntax.Proto2);
	}

	/// <summary>
	/// Suggest a .proto definition for the given type
	/// </summary>
	/// <typeparam name="T">The type to generate a .proto definition for</typeparam>
	/// <returns>The .proto definition as a string</returns>
	public static string GetProto<T>(ProtoSyntax syntax)
	{
		return RuntimeTypeModel.Default.GetSchema(RuntimeTypeModel.Default.MapType(typeof(T)), syntax);
	}

	/// <summary>
	/// Create a deep clone of the supplied instance; any sub-items are also cloned.
	/// </summary>
	public static T DeepClone<T>(T instance)
	{
		if (instance != null)
		{
			return (T)RuntimeTypeModel.Default.DeepClone(instance);
		}
		return instance;
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance.
	/// </summary>
	/// <typeparam name="T">The type being merged.</typeparam>
	/// <param name="instance">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public static T Merge<T>(Stream source, T instance)
	{
		return (T)RuntimeTypeModel.Default.Deserialize(source, instance, typeof(T));
	}

	/// <summary>
	/// Creates a new instance from a protocol-buffer stream
	/// </summary>
	/// <typeparam name="T">The type to be created.</typeparam>
	/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
	/// <returns>A new, initialized instance.</returns>
	public static T Deserialize<T>(Stream source)
	{
		return (T)RuntimeTypeModel.Default.Deserialize(source, null, typeof(T));
	}

	/// <summary>
	/// Creates a new instance from a protocol-buffer stream
	/// </summary>
	/// <param name="type">The type to be created.</param>
	/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
	/// <returns>A new, initialized instance.</returns>
	public static object Deserialize(Type type, Stream source)
	{
		return RuntimeTypeModel.Default.Deserialize(source, null, type);
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream.
	/// </summary>
	/// <param name="instance">The existing instance to be serialized (cannot be null).</param>
	/// <param name="destination">The destination stream to write to.</param>
	public static void Serialize<T>(Stream destination, T instance)
	{
		if (instance != null)
		{
			RuntimeTypeModel.Default.Serialize(destination, instance);
		}
	}

	/// <summary>
	/// Serializes a given instance and deserializes it as a different type;
	/// this can be used to translate between wire-compatible objects (where
	/// two .NET types represent the same data), or to promote/demote a type
	/// through an inheritance hierarchy.
	/// </summary>
	/// <remarks>No assumption of compatibility is made between the types.</remarks>
	/// <typeparam name="TFrom">The type of the object being copied.</typeparam>
	/// <typeparam name="TTo">The type of the new object to be created.</typeparam>
	/// <param name="instance">The existing instance to use as a template.</param>
	/// <returns>A new instane of type TNewType, with the data from TOldType.</returns>
	public static TTo ChangeType<TFrom, TTo>(TFrom instance)
	{
		using MemoryStream memoryStream = new MemoryStream();
		Serialize(memoryStream, instance);
		memoryStream.Position = 0L;
		return Deserialize<TTo>(memoryStream);
	}

	/// <summary>
	/// Precompiles the serializer for a given type.
	/// </summary>
	public static void PrepareSerializer<T>()
	{
		NonGeneric.PrepareSerializer(typeof(T));
	}

	/// <summary>
	/// Reads a sequence of consecutive length-prefixed items from a stream, using
	/// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
	/// are directly comparable to serializing multiple items in succession
	/// (use the <see cref="F:ProtoBuf.Serializer.ListItemTag" /> tag to emulate the implicit behavior
	/// when serializing a list/array). When a tag is
	/// specified, any records with different tags are silently omitted. The
	/// tag is ignored. The tag is ignored for fixed-length prefixes.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize.</typeparam>
	/// <param name="source">The binary stream containing the serialized records.</param>
	/// <param name="style">The prefix style used in the data.</param>
	/// <param name="fieldNumber">
	/// The tag of records to return (if non-positive, then no tag is
	/// expected and all records are returned).
	/// </param>
	/// <returns>The sequence of deserialized objects.</returns>
	public static IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int fieldNumber)
	{
		return RuntimeTypeModel.Default.DeserializeItems<T>(source, style, fieldNumber);
	}

	/// <summary>
	/// Creates a new instance from a protocol-buffer stream that has a length-prefix
	/// on data (to assist with network IO).
	/// </summary>
	/// <typeparam name="T">The type to be created.</typeparam>
	/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <returns>A new, initialized instance.</returns>
	public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style)
	{
		return DeserializeWithLengthPrefix<T>(source, style, 0);
	}

	/// <summary>
	/// Creates a new instance from a protocol-buffer stream that has a length-prefix
	/// on data (to assist with network IO).
	/// </summary>
	/// <typeparam name="T">The type to be created.</typeparam>
	/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="fieldNumber">The expected tag of the item (only used with base-128 prefix style).</param>
	/// <returns>A new, initialized instance.</returns>
	public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style, int fieldNumber)
	{
		RuntimeTypeModel @default = RuntimeTypeModel.Default;
		return (T)@default.DeserializeWithLengthPrefix(source, null, @default.MapType(typeof(T)), style, fieldNumber);
	}

	/// <summary>
	/// Applies a protocol-buffer stream to an existing instance, using length-prefixed
	/// data - useful with network IO.
	/// </summary>
	/// <typeparam name="T">The type being merged.</typeparam>
	/// <param name="instance">The existing instance to be modified (can be null).</param>
	/// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <returns>
	/// The updated instance; this may be different to the instance argument if
	/// either the original instance was null, or the stream defines a known sub-type of the
	/// original instance.
	/// </returns>
	public static T MergeWithLengthPrefix<T>(Stream source, T instance, PrefixStyle style)
	{
		RuntimeTypeModel @default = RuntimeTypeModel.Default;
		return (T)@default.DeserializeWithLengthPrefix(source, instance, @default.MapType(typeof(T)), style, 0);
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream,
	/// with a length-prefix. This is useful for socket programming,
	/// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
	/// from an ongoing stream.
	/// </summary>
	/// <typeparam name="T">The type being serialized.</typeparam>
	/// <param name="instance">The existing instance to be serialized (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="destination">The destination stream to write to.</param>
	public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style)
	{
		SerializeWithLengthPrefix(destination, instance, style, 0);
	}

	/// <summary>
	/// Writes a protocol-buffer representation of the given instance to the supplied stream,
	/// with a length-prefix. This is useful for socket programming,
	/// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
	/// from an ongoing stream.
	/// </summary>
	/// <typeparam name="T">The type being serialized.</typeparam>
	/// <param name="instance">The existing instance to be serialized (cannot be null).</param>
	/// <param name="style">How to encode the length prefix.</param>
	/// <param name="destination">The destination stream to write to.</param>
	/// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
	public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style, int fieldNumber)
	{
		RuntimeTypeModel @default = RuntimeTypeModel.Default;
		@default.SerializeWithLengthPrefix(destination, instance, @default.MapType(typeof(T)), style, fieldNumber);
	}

	/// <summary>Indicates the number of bytes expected for the next message.</summary>
	/// <param name="source">The stream containing the data to investigate for a length.</param>
	/// <param name="style">The algorithm used to encode the length.</param>
	/// <param name="length">The length of the message, if it could be identified.</param>
	/// <returns>True if a length could be obtained, false otherwise.</returns>
	public static bool TryReadLengthPrefix(Stream source, PrefixStyle style, out int length)
	{
		length = ProtoReader.ReadLengthPrefix(source, expectHeader: false, style, out var _, out var bytesRead);
		return bytesRead > 0;
	}

	/// <summary>Indicates the number of bytes expected for the next message.</summary>
	/// <param name="buffer">The buffer containing the data to investigate for a length.</param>
	/// <param name="index">The offset of the first byte to read from the buffer.</param>
	/// <param name="count">The number of bytes to read from the buffer.</param>
	/// <param name="style">The algorithm used to encode the length.</param>
	/// <param name="length">The length of the message, if it could be identified.</param>
	/// <returns>True if a length could be obtained, false otherwise.</returns>
	public static bool TryReadLengthPrefix(byte[] buffer, int index, int count, PrefixStyle style, out int length)
	{
		using Stream source = new MemoryStream(buffer, index, count);
		return TryReadLengthPrefix(source, style, out length);
	}

	/// <summary>
	/// Releases any internal buffers that have been reserved for efficiency; this does not affect any serialization
	/// operations; simply: it can be used (optionally) to release the buffers for garbage collection (at the expense
	/// of having to re-allocate a new buffer for the next operation, rather than re-use prior buffers).
	/// </summary>
	public static void FlushPool()
	{
		BufferPool.Flush();
	}
}
