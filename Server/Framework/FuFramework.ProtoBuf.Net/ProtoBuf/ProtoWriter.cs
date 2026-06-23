using System;
using System.IO;
using System.Text;
using ProtoBuf.Meta;

namespace ProtoBuf;

/// <summary>
/// Represents an output stream for writing protobuf data.
/// Why is the API backwards (static methods with writer arguments)?
/// See: http://marcgravell.blogspot.com/2010/03/last-will-be-first-and-first-will-be.html
/// </summary>
public sealed class ProtoWriter : IDisposable
{
	private Stream dest;

	private int fieldNumber;

	private int flushLock;

	private int depth;

	private const int RecursionCheckDepth = 25;

	private MutableList recursionStack;

	private byte[] ioBuffer;

	private int ioIndex;

	private long position64;

	private static readonly UTF8Encoding encoding = new UTF8Encoding();

	private int packedFieldNumber;

	internal NetObjectCache NetCache { get; } = new NetObjectCache();

	internal WireType WireType { get; private set; }

	/// <summary>
	/// Addition information about this serialization operation.
	/// </summary>
	public SerializationContext Context { get; }

	/// <summary>
	/// Get the TypeModel associated with this writer
	/// </summary>
	public TypeModel Model { get; private set; }

	/// <summary>
	/// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type).
	/// </summary>
	/// <param name="value">The object to write.</param>
	/// <param name="key">The key that uniquely identifies the type within the model.</param>
	/// <param name="writer">The destination.</param>
	public static void WriteObject(object value, int key, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (writer.Model == null)
		{
			throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
		}
		SubItemToken token = StartSubItem(value, writer);
		if (key >= 0)
		{
			writer.Model.Serialize(key, value, writer);
		}
		else if (writer.Model == null || !writer.Model.TrySerializeAuxiliaryType(writer, value.GetType(), DataFormat.Default, 1, value, isInsideList: false, null))
		{
			TypeModel.ThrowUnexpectedType(value.GetType());
		}
		EndSubItem(token, writer);
	}

	/// <summary>
	/// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type) - but the
	/// caller is asserting that this relationship is non-recursive; no recursion check will be
	/// performed.
	/// </summary>
	/// <param name="value">The object to write.</param>
	/// <param name="key">The key that uniquely identifies the type within the model.</param>
	/// <param name="writer">The destination.</param>
	public static void WriteRecursionSafeObject(object value, int key, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (writer.Model == null)
		{
			throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
		}
		SubItemToken token = StartSubItem(null, writer);
		writer.Model.Serialize(key, value, writer);
		EndSubItem(token, writer);
	}

	internal static void WriteObject(object value, int key, ProtoWriter writer, PrefixStyle style, int fieldNumber)
	{
		if (writer.Model == null)
		{
			throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
		}
		if (writer.WireType != WireType.None)
		{
			throw CreateException(writer);
		}
		switch (style)
		{
		case PrefixStyle.Base128:
			writer.WireType = WireType.String;
			writer.fieldNumber = fieldNumber;
			if (fieldNumber > 0)
			{
				WriteHeaderCore(fieldNumber, WireType.String, writer);
			}
			break;
		case PrefixStyle.Fixed32:
		case PrefixStyle.Fixed32BigEndian:
			writer.fieldNumber = 0;
			writer.WireType = WireType.Fixed32;
			break;
		default:
			throw new ArgumentOutOfRangeException("style");
		}
		SubItemToken token = StartSubItem(value, writer, allowFixed: true);
		if (key < 0)
		{
			if (!writer.Model.TrySerializeAuxiliaryType(writer, value.GetType(), DataFormat.Default, 1, value, isInsideList: false, null))
			{
				TypeModel.ThrowUnexpectedType(value.GetType());
			}
		}
		else
		{
			writer.Model.Serialize(key, value, writer);
		}
		EndSubItem(token, writer, style);
	}

	internal int GetTypeKey(ref Type type)
	{
		return Model.GetKey(ref type);
	}

	/// <summary>
	/// Writes a field-header, indicating the format of the next data we plan to write.
	/// </summary>
	public static void WriteFieldHeader(int fieldNumber, WireType wireType, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (writer.WireType != WireType.None)
		{
			throw new InvalidOperationException("Cannot write a " + wireType.ToString() + " header until the " + writer.WireType.ToString() + " data has been written");
		}
		if (fieldNumber < 0)
		{
			throw new ArgumentOutOfRangeException("fieldNumber");
		}
		if (writer.packedFieldNumber == 0)
		{
			writer.fieldNumber = fieldNumber;
			writer.WireType = wireType;
			WriteHeaderCore(fieldNumber, wireType, writer);
			return;
		}
		if (writer.packedFieldNumber == fieldNumber)
		{
			if ((uint)wireType > 1u && wireType != WireType.Fixed32 && wireType != WireType.SignedVariant)
			{
				throw new InvalidOperationException("Wire-type cannot be encoded as packed: " + wireType);
			}
			writer.fieldNumber = fieldNumber;
			writer.WireType = wireType;
			return;
		}
		throw new InvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber + " but received " + fieldNumber);
	}

	internal static void WriteHeaderCore(int fieldNumber, WireType wireType, ProtoWriter writer)
	{
		WriteUInt32Variant((uint)(fieldNumber << 3) | (uint)(wireType & (WireType)7), writer);
	}

	/// <summary>
	/// Writes a byte-array to the stream; supported wire-types: String
	/// </summary>
	public static void WriteBytes(byte[] data, ProtoWriter writer)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		WriteBytes(data, 0, data.Length, writer);
	}

	/// <summary>
	/// Writes a byte-array to the stream; supported wire-types: String
	/// </summary>
	public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed32:
			if (length != 4)
			{
				throw new ArgumentException("length");
			}
			break;
		case WireType.Fixed64:
			if (length != 8)
			{
				throw new ArgumentException("length");
			}
			break;
		case WireType.String:
			WriteUInt32Variant((uint)length, writer);
			writer.WireType = WireType.None;
			if (length == 0)
			{
				return;
			}
			if (writer.flushLock == 0 && length > writer.ioBuffer.Length)
			{
				Flush(writer);
				writer.dest.Write(data, offset, length);
				writer.position64 += length;
				return;
			}
			break;
		default:
			throw CreateException(writer);
		}
		DemandSpace(length, writer);
		Buffer.BlockCopy(data, offset, writer.ioBuffer, writer.ioIndex, length);
		IncrementedAndReset(length, writer);
	}

	private static void CopyRawFromStream(Stream source, ProtoWriter writer)
	{
		byte[] array = writer.ioBuffer;
		int num = array.Length - writer.ioIndex;
		int num2 = 1;
		while (num > 0 && (num2 = source.Read(array, writer.ioIndex, num)) > 0)
		{
			writer.ioIndex += num2;
			writer.position64 += num2;
			num -= num2;
		}
		if (num2 <= 0)
		{
			return;
		}
		if (writer.flushLock == 0)
		{
			Flush(writer);
			while ((num2 = source.Read(array, 0, array.Length)) > 0)
			{
				writer.dest.Write(array, 0, num2);
				writer.position64 += num2;
			}
			return;
		}
		while (true)
		{
			DemandSpace(128, writer);
			if ((num2 = source.Read(writer.ioBuffer, writer.ioIndex, writer.ioBuffer.Length - writer.ioIndex)) > 0)
			{
				writer.position64 += num2;
				writer.ioIndex += num2;
				continue;
			}
			break;
		}
	}

	private static void IncrementedAndReset(int length, ProtoWriter writer)
	{
		writer.ioIndex += length;
		writer.position64 += length;
		writer.WireType = WireType.None;
	}

	/// <summary>
	/// Indicates the start of a nested record.
	/// </summary>
	/// <param name="instance">The instance to write.</param>
	/// <param name="writer">The destination.</param>
	/// <returns>A token representing the state of the stream; this token is given to EndSubItem.</returns>
	public static SubItemToken StartSubItem(object instance, ProtoWriter writer)
	{
		return StartSubItem(instance, writer, allowFixed: false);
	}

	private void CheckRecursionStackAndPush(object instance)
	{
		int num;
		if (recursionStack == null)
		{
			recursionStack = new MutableList();
		}
		else if (instance != null && (num = recursionStack.IndexOfReference(instance)) >= 0)
		{
			throw new ProtoException("Possible recursion detected (offset: " + (recursionStack.Count - num) + " level(s)): " + instance);
		}
		recursionStack.Add(instance);
	}

	private void PopRecursionStack()
	{
		recursionStack.RemoveLast();
	}

	private static SubItemToken StartSubItem(object instance, ProtoWriter writer, bool allowFixed)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (++writer.depth > 25)
		{
			writer.CheckRecursionStackAndPush(instance);
		}
		if (writer.packedFieldNumber != 0)
		{
			throw new InvalidOperationException("Cannot begin a sub-item while performing packed encoding");
		}
		switch (writer.WireType)
		{
		case WireType.StartGroup:
			writer.WireType = WireType.None;
			return new SubItemToken((long)(-writer.fieldNumber));
		case WireType.String:
			writer.WireType = WireType.None;
			DemandSpace(32, writer);
			writer.flushLock++;
			writer.position64++;
			return new SubItemToken((long)writer.ioIndex++);
		case WireType.Fixed32:
		{
			if (!allowFixed)
			{
				throw CreateException(writer);
			}
			DemandSpace(32, writer);
			writer.flushLock++;
			SubItemToken result = new SubItemToken((long)writer.ioIndex);
			IncrementedAndReset(4, writer);
			return result;
		}
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Indicates the end of a nested record.
	/// </summary>
	/// <param name="token">The token obtained from StartubItem.</param>
	/// <param name="writer">The destination.</param>
	public static void EndSubItem(SubItemToken token, ProtoWriter writer)
	{
		EndSubItem(token, writer, PrefixStyle.Base128);
	}

	private static void EndSubItem(SubItemToken token, ProtoWriter writer, PrefixStyle style)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (writer.WireType != WireType.None)
		{
			throw CreateException(writer);
		}
		int num = (int)token.value64;
		if (writer.depth <= 0)
		{
			throw CreateException(writer);
		}
		if (writer.depth-- > 25)
		{
			writer.PopRecursionStack();
		}
		writer.packedFieldNumber = 0;
		if (num < 0)
		{
			WriteHeaderCore(-num, WireType.EndGroup, writer);
			writer.WireType = WireType.None;
			return;
		}
		switch (style)
		{
		case PrefixStyle.Fixed32:
		{
			int num2 = writer.ioIndex - num - 4;
			WriteInt32ToBuffer(num2, writer.ioBuffer, num);
			break;
		}
		case PrefixStyle.Fixed32BigEndian:
		{
			int num2 = writer.ioIndex - num - 4;
			byte[] array2 = writer.ioBuffer;
			WriteInt32ToBuffer(num2, array2, num);
			byte b = array2[num];
			array2[num] = array2[num + 3];
			array2[num + 3] = b;
			b = array2[num + 1];
			array2[num + 1] = array2[num + 2];
			array2[num + 2] = b;
			break;
		}
		case PrefixStyle.Base128:
		{
			int num2 = writer.ioIndex - num - 1;
			int num3 = 0;
			uint num4 = (uint)num2;
			while ((num4 >>= 7) != 0)
			{
				num3++;
			}
			if (num3 == 0)
			{
				writer.ioBuffer[num] = (byte)(num2 & 0x7F);
				break;
			}
			DemandSpace(num3, writer);
			byte[] array = writer.ioBuffer;
			Buffer.BlockCopy(array, num + 1, array, num + 1 + num3, num2);
			num4 = (uint)num2;
			do
			{
				array[num++] = (byte)((num4 & 0x7F) | 0x80);
			}
			while ((num4 >>= 7) != 0);
			array[num - 1] = (byte)(array[num - 1] & -129);
			writer.position64 += num3;
			writer.ioIndex += num3;
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("style");
		}
		if (--writer.flushLock == 0 && writer.ioIndex >= 1024)
		{
			Flush(writer);
		}
	}

	/// <summary>
	/// Creates a new writer against a stream
	/// </summary>
	/// <param name="dest">The destination stream</param>
	/// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
	/// <param name="context">Additional context about this serialization operation</param>
	public static ProtoWriter Create(Stream dest, TypeModel model, SerializationContext context = null)
	{
		return new ProtoWriter(dest, model, context);
	}

	/// <summary>
	/// Creates a new writer against a stream
	/// </summary>
	/// <param name="dest">The destination stream</param>
	/// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
	/// <param name="context">Additional context about this serialization operation</param>
	[Obsolete("Please use ProtoWriter.Create; this API may be removed in a future version", false)]
	public ProtoWriter(Stream dest, TypeModel model, SerializationContext context)
	{
		if (dest == null)
		{
			throw new ArgumentNullException("dest");
		}
		if (!dest.CanWrite)
		{
			throw new ArgumentException("Cannot write to stream", "dest");
		}
		this.dest = dest;
		ioBuffer = BufferPool.GetBuffer();
		Model = model;
		WireType = WireType.None;
		if (context == null)
		{
			context = SerializationContext.Default;
		}
		else
		{
			context.Freeze();
		}
		Context = context;
	}

	void IDisposable.Dispose()
	{
		Dispose();
	}

	private void Dispose()
	{
		if (dest != null)
		{
			Flush(this);
			dest = null;
		}
		Model = null;
		BufferPool.ReleaseBufferToPool(ref ioBuffer);
	}

	internal static long GetLongPosition(ProtoWriter writer)
	{
		return writer.position64;
	}

	internal static int GetPosition(ProtoWriter writer)
	{
		return checked((int)writer.position64);
	}

	private static void DemandSpace(int required, ProtoWriter writer)
	{
		if (writer.ioBuffer.Length - writer.ioIndex < required)
		{
			TryFlushOrResize(required, writer);
		}
	}

	private static void TryFlushOrResize(int required, ProtoWriter writer)
	{
		if (writer.flushLock == 0)
		{
			Flush(writer);
			if (writer.ioBuffer.Length - writer.ioIndex >= required)
			{
				return;
			}
		}
		BufferPool.ResizeAndFlushLeft(ref writer.ioBuffer, required + writer.ioIndex, 0, writer.ioIndex);
	}

	/// <summary>
	/// Flushes data to the underlying stream, and releases any resources. The underlying stream is *not* disposed
	/// by this operation.
	/// </summary>
	public void Close()
	{
		if (depth != 0 || flushLock != 0)
		{
			throw new InvalidOperationException("Unable to close stream in an incomplete state");
		}
		Dispose();
	}

	internal void CheckDepthFlushlock()
	{
		if (depth != 0 || flushLock != 0)
		{
			throw new InvalidOperationException("The writer is in an incomplete state");
		}
	}

	/// <summary>
	/// Writes any buffered data (if possible) to the underlying stream.
	/// </summary>
	/// <param name="writer">The writer to flush</param>
	/// <remarks>
	/// It is not always possible to fully flush, since some sequences
	/// may require values to be back-filled into the byte-stream.
	/// </remarks>
	internal static void Flush(ProtoWriter writer)
	{
		if (writer.flushLock == 0 && writer.ioIndex != 0)
		{
			writer.dest.Write(writer.ioBuffer, 0, writer.ioIndex);
			writer.ioIndex = 0;
		}
	}

	/// <summary>
	/// Writes an unsigned 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	private static void WriteUInt32Variant(uint value, ProtoWriter writer)
	{
		DemandSpace(5, writer);
		int num = 0;
		do
		{
			writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7F) | 0x80);
			num++;
		}
		while ((value >>= 7) != 0);
		writer.ioBuffer[writer.ioIndex - 1] &= 127;
		writer.position64 += num;
	}

	internal static uint Zig(int value)
	{
		return (uint)((value << 1) ^ (value >> 31));
	}

	internal static ulong Zig(long value)
	{
		return (ulong)((value << 1) ^ (value >> 63));
	}

	private static void WriteUInt64Variant(ulong value, ProtoWriter writer)
	{
		DemandSpace(10, writer);
		int num = 0;
		do
		{
			writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7F) | 0x80);
			num++;
		}
		while ((value >>= 7) != 0L);
		writer.ioBuffer[writer.ioIndex - 1] &= 127;
		writer.position64 += num;
	}

	/// <summary>
	/// Writes a string to the stream; supported wire-types: String
	/// </summary>
	public static void WriteString(string value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (writer.WireType != WireType.String)
		{
			throw CreateException(writer);
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (value.Length == 0)
		{
			WriteUInt32Variant(0u, writer);
			writer.WireType = WireType.None;
			return;
		}
		int byteCount = encoding.GetByteCount(value);
		WriteUInt32Variant((uint)byteCount, writer);
		DemandSpace(byteCount, writer);
		IncrementedAndReset(encoding.GetBytes(value, 0, value.Length, writer.ioBuffer, writer.ioIndex), writer);
	}

	/// <summary>
	/// Writes an unsigned 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public static void WriteUInt64(ulong value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed64:
			WriteInt64((long)value, writer);
			break;
		case WireType.Variant:
			WriteUInt64Variant(value, writer);
			writer.WireType = WireType.None;
			break;
		case WireType.Fixed32:
			WriteUInt32(checked((uint)value), writer);
			break;
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Writes a signed 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public static void WriteInt64(long value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed64:
		{
			DemandSpace(8, writer);
			byte[] array2 = writer.ioBuffer;
			int num = writer.ioIndex;
			array2[num] = (byte)value;
			array2[num + 1] = (byte)(value >> 8);
			array2[num + 2] = (byte)(value >> 16);
			array2[num + 3] = (byte)(value >> 24);
			array2[num + 4] = (byte)(value >> 32);
			array2[num + 5] = (byte)(value >> 40);
			array2[num + 6] = (byte)(value >> 48);
			array2[num + 7] = (byte)(value >> 56);
			IncrementedAndReset(8, writer);
			break;
		}
		case WireType.SignedVariant:
			WriteUInt64Variant(Zig(value), writer);
			writer.WireType = WireType.None;
			break;
		case WireType.Variant:
		{
			if (value >= 0)
			{
				WriteUInt64Variant((ulong)value, writer);
				writer.WireType = WireType.None;
				break;
			}
			DemandSpace(10, writer);
			byte[] array = writer.ioBuffer;
			int num = writer.ioIndex;
			array[num] = (byte)(value | 0x80);
			array[num + 1] = (byte)((int)(value >> 7) | 0x80);
			array[num + 2] = (byte)((int)(value >> 14) | 0x80);
			array[num + 3] = (byte)((int)(value >> 21) | 0x80);
			array[num + 4] = (byte)((int)(value >> 28) | 0x80);
			array[num + 5] = (byte)((int)(value >> 35) | 0x80);
			array[num + 6] = (byte)((int)(value >> 42) | 0x80);
			array[num + 7] = (byte)((int)(value >> 49) | 0x80);
			array[num + 8] = (byte)((int)(value >> 56) | 0x80);
			array[num + 9] = 1;
			IncrementedAndReset(10, writer);
			break;
		}
		case WireType.Fixed32:
			WriteInt32(checked((int)value), writer);
			break;
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public static void WriteUInt32(uint value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed32:
			WriteInt32((int)value, writer);
			break;
		case WireType.Fixed64:
			WriteInt64((int)value, writer);
			break;
		case WireType.Variant:
			WriteUInt32Variant(value, writer);
			writer.WireType = WireType.None;
			break;
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Writes a signed 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public static void WriteInt16(short value, ProtoWriter writer)
	{
		WriteInt32(value, writer);
	}

	/// <summary>
	/// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public static void WriteUInt16(ushort value, ProtoWriter writer)
	{
		WriteUInt32(value, writer);
	}

	/// <summary>
	/// Writes an unsigned 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public static void WriteByte(byte value, ProtoWriter writer)
	{
		WriteUInt32(value, writer);
	}

	/// <summary>
	/// Writes a signed 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public static void WriteSByte(sbyte value, ProtoWriter writer)
	{
		WriteInt32(value, writer);
	}

	private static void WriteInt32ToBuffer(int value, byte[] buffer, int index)
	{
		buffer[index] = (byte)value;
		buffer[index + 1] = (byte)(value >> 8);
		buffer[index + 2] = (byte)(value >> 16);
		buffer[index + 3] = (byte)(value >> 24);
	}

	/// <summary>
	/// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public static void WriteInt32(int value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed32:
			DemandSpace(4, writer);
			WriteInt32ToBuffer(value, writer.ioBuffer, writer.ioIndex);
			IncrementedAndReset(4, writer);
			break;
		case WireType.Fixed64:
		{
			DemandSpace(8, writer);
			byte[] array = writer.ioBuffer;
			int num = writer.ioIndex;
			array[num] = (byte)value;
			array[num + 1] = (byte)(value >> 8);
			array[num + 2] = (byte)(value >> 16);
			array[num + 3] = (byte)(value >> 24);
			array[num + 4] = (array[num + 5] = (array[num + 6] = (array[num + 7] = 0)));
			IncrementedAndReset(8, writer);
			break;
		}
		case WireType.SignedVariant:
			WriteUInt32Variant(Zig(value), writer);
			writer.WireType = WireType.None;
			break;
		case WireType.Variant:
		{
			if (value >= 0)
			{
				WriteUInt32Variant((uint)value, writer);
				writer.WireType = WireType.None;
				break;
			}
			DemandSpace(10, writer);
			byte[] array = writer.ioBuffer;
			int num = writer.ioIndex;
			array[num] = (byte)(value | 0x80);
			array[num + 1] = (byte)((value >> 7) | 0x80);
			array[num + 2] = (byte)((value >> 14) | 0x80);
			array[num + 3] = (byte)((value >> 21) | 0x80);
			array[num + 4] = (byte)((value >> 28) | 0x80);
			byte[] array2 = array;
			int num2 = num + 5;
			byte[] array3 = array;
			int num3 = num + 6;
			byte[] array4 = array;
			int num4 = num + 7;
			byte b;
			array[num + 8] = (b = byte.MaxValue);
			array2[num2] = (array3[num3] = (array4[num4] = b));
			array[num + 9] = 1;
			IncrementedAndReset(10, writer);
			break;
		}
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Writes a double-precision number to the stream; supported wire-types: Fixed32, Fixed64
	/// </summary>
	public unsafe static void WriteDouble(double value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed32:
		{
			float num = (float)value;
			if (float.IsInfinity(num) && !double.IsInfinity(value))
			{
				throw new OverflowException();
			}
			WriteSingle(num, writer);
			break;
		}
		case WireType.Fixed64:
			WriteInt64(*(long*)(&value), writer);
			break;
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Writes a single-precision number to the stream; supported wire-types: Fixed32, Fixed64
	/// </summary>
	public unsafe static void WriteSingle(float value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		switch (writer.WireType)
		{
		case WireType.Fixed32:
			WriteInt32(*(int*)(&value), writer);
			break;
		case WireType.Fixed64:
			WriteDouble(value, writer);
			break;
		default:
			throw CreateException(writer);
		}
	}

	/// <summary>
	/// Throws an exception indicating that the given enum cannot be mapped to a serialized value.
	/// </summary>
	public static void ThrowEnumException(ProtoWriter writer, object enumValue)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		string text = ((enumValue == null) ? "<null>" : (enumValue.GetType().FullName + "." + enumValue));
		throw new ProtoException("No wire-value is mapped to the enum " + text + " at position " + writer.position64);
	}

	internal static Exception CreateException(ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		return new ProtoException("Invalid serialization operation with wire-type " + writer.WireType.ToString() + " at position " + writer.position64);
	}

	/// <summary>
	/// Writes a boolean to the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public static void WriteBoolean(bool value, ProtoWriter writer)
	{
		WriteUInt32(value ? 1u : 0u, writer);
	}

	/// <summary>
	/// Copies any extension data stored for the instance to the underlying stream
	/// </summary>
	public static void AppendExtensionData(IExtensible instance, ProtoWriter writer)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (writer.WireType != WireType.None)
		{
			throw CreateException(writer);
		}
		IExtension extensionObject = instance.GetExtensionObject(createIfMissing: false);
		if (extensionObject != null)
		{
			Stream stream = extensionObject.BeginQuery();
			try
			{
				CopyRawFromStream(stream, writer);
			}
			finally
			{
				extensionObject.EndQuery(stream);
			}
		}
	}

	/// <summary>
	/// Used for packed encoding; indicates that the next field should be skipped rather than
	/// a field header written. Note that the field number must match, else an exception is thrown
	/// when the attempt is made to write the (incorrect) field. The wire-type is taken from the
	/// subsequent call to WriteFieldHeader. Only primitive types can be packed.
	/// </summary>
	public static void SetPackedField(int fieldNumber, ProtoWriter writer)
	{
		if (fieldNumber <= 0)
		{
			throw new ArgumentOutOfRangeException("fieldNumber");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.packedFieldNumber = fieldNumber;
	}

	/// <summary>
	/// Used for packed encoding; explicitly reset the packed field marker; this is not required
	/// if using StartSubItem/EndSubItem
	/// </summary>
	public static void ClearPackedField(int fieldNumber, ProtoWriter writer)
	{
		if (fieldNumber != writer.packedFieldNumber)
		{
			throw new InvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber + " but received " + fieldNumber);
		}
		writer.packedFieldNumber = 0;
	}

	/// <summary>
	/// Used for packed encoding; writes the length prefix using fixed sizes rather than using
	/// buffering. Only valid for fixed-32 and fixed-64 encoding.
	/// </summary>
	public static void WritePackedPrefix(int elementCount, WireType wireType, ProtoWriter writer)
	{
		if (writer.WireType != WireType.String)
		{
			throw new InvalidOperationException("Invalid wire-type: " + writer.WireType);
		}
		if (elementCount < 0)
		{
			throw new ArgumentOutOfRangeException("elementCount");
		}
		WriteUInt64Variant(wireType switch
		{
			WireType.Fixed32 => (ulong)((long)elementCount << 2), 
			WireType.Fixed64 => (ulong)((long)elementCount << 3), 
			_ => throw new ArgumentOutOfRangeException("wireType", "Invalid wire-type: " + wireType), 
		}, writer);
		writer.WireType = WireType.None;
	}

	internal string SerializeType(Type type)
	{
		return TypeModel.SerializeType(Model, type);
	}

	/// <summary>
	/// Specifies a known root object to use during reference-tracked serialization
	/// </summary>
	public void SetRootObject(object value)
	{
		NetCache.SetKeyedObject(0, value);
	}

	/// <summary>
	/// Writes a Type to the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
	/// </summary>
	public static void WriteType(Type value, ProtoWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		WriteString(writer.SerializeType(value), writer);
	}
}
