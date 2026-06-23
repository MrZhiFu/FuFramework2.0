using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf.Meta;

namespace ProtoBuf;

/// <summary>
/// A stateful reader, used to read a protobuf stream. Typical usage would be (sequentially) to call
/// ReadFieldHeader and (after matching the field) an appropriate Read* method.
/// </summary>
public sealed class ProtoReader : IDisposable
{
	private Stream source;

	private byte[] ioBuffer;

	private int depth;

	private int ioIndex;

	private int available;

	private long blockEnd64;

	private long dataRemaining64;

	private bool isFixedLength;

	private uint trapCount;

	internal const long TO_EOF = -1L;

	private const long Int64Msb = long.MinValue;

	private const int Int32Msb = int.MinValue;

	private Dictionary<string, string> stringInterner;

	private static readonly UTF8Encoding encoding = new UTF8Encoding();

	private static readonly byte[] EmptyBlob = new byte[0];

	[ThreadStatic]
	private static ProtoReader lastReader;

	/// <summary>
	/// Gets the number of the field being processed.
	/// </summary>
	public int FieldNumber { get; private set; }

	/// <summary>
	/// Indicates the underlying proto serialization format on the wire.
	/// </summary>
	public WireType WireType { get; private set; }

	/// <summary>
	/// Gets / sets a flag indicating whether strings should be checked for repetition; if
	/// true, any repeated UTF-8 byte sequence will result in the same String instance, rather
	/// than a second instance of the same string. Enabled by default. Note that this uses
	/// a <i>custom</i> interner - the system-wide string interner is not used.
	/// </summary>
	public bool InternStrings { get; set; }

	/// <summary>
	/// Addition information about this deserialization operation.
	/// </summary>
	public SerializationContext Context { get; private set; }

	/// <summary>
	/// Returns the position of the current reader (note that this is not necessarily the same as the position
	/// in the underlying stream, if multiple readers are used on the same stream)
	/// </summary>
	public int Position => checked((int)LongPosition);

	/// <summary>
	/// Returns the position of the current reader (note that this is not necessarily the same as the position
	/// in the underlying stream, if multiple readers are used on the same stream)
	/// </summary>
	public long LongPosition { get; private set; }

	/// <summary>
	/// Get the TypeModel associated with this reader
	/// </summary>
	public TypeModel Model { get; private set; }

	internal NetObjectCache NetCache { get; private set; }

	/// <summary>
	/// Creates a new reader against a stream
	/// </summary>
	/// <param name="source">The source stream</param>
	/// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
	/// <param name="context">Additional context about this serialization operation</param>
	[Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", false)]
	public ProtoReader(Stream source, TypeModel model, SerializationContext context)
	{
		Init(this, source, model, context, -1L);
	}

	/// <summary>
	/// Creates a new reader against a stream
	/// </summary>
	/// <param name="source">The source stream</param>
	/// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
	/// <param name="context">Additional context about this serialization operation</param>
	/// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
	[Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", false)]
	public ProtoReader(Stream source, TypeModel model, SerializationContext context, int length)
	{
		Init(this, source, model, context, length);
	}

	/// <summary>
	/// Creates a new reader against a stream
	/// </summary>
	/// <param name="source">The source stream</param>
	/// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
	/// <param name="context">Additional context about this serialization operation</param>
	/// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
	[Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", false)]
	public ProtoReader(Stream source, TypeModel model, SerializationContext context, long length)
	{
		Init(this, source, model, context, length);
	}

	private static void Init(ProtoReader reader, Stream source, TypeModel model, SerializationContext context, long length)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (!source.CanRead)
		{
			throw new ArgumentException("Cannot read from stream", "source");
		}
		reader.source = source;
		reader.ioBuffer = BufferPool.GetBuffer();
		reader.Model = model;
		reader.dataRemaining64 = ((reader.isFixedLength = length >= 0) ? length : 0);
		if (context == null)
		{
			context = SerializationContext.Default;
		}
		else
		{
			context.Freeze();
		}
		reader.Context = context;
		reader.LongPosition = 0L;
		int num2 = (reader.FieldNumber = (reader.ioIndex = 0));
		reader.available = (reader.depth = num2);
		reader.blockEnd64 = long.MaxValue;
		reader.InternStrings = RuntimeTypeModel.Default.InternStrings;
		reader.WireType = WireType.None;
		reader.trapCount = 1u;
		if (reader.NetCache == null)
		{
			reader.NetCache = new NetObjectCache();
		}
	}

	/// <summary>
	/// Releases resources used by the reader, but importantly <b>does not</b> Dispose the
	/// underlying stream; in many typical use-cases the stream is used for different
	/// processes, so it is assumed that the consumer will Dispose their stream separately.
	/// </summary>
	public void Dispose()
	{
		source = null;
		Model = null;
		BufferPool.ReleaseBufferToPool(ref ioBuffer);
		if (stringInterner != null)
		{
			stringInterner.Clear();
			stringInterner = null;
		}
		if (NetCache != null)
		{
			NetCache.Clear();
		}
	}

	internal int TryReadUInt32VariantWithoutMoving(bool trimNegative, out uint value)
	{
		if (available < 10)
		{
			Ensure(10, strict: false);
		}
		if (available == 0)
		{
			value = 0u;
			return 0;
		}
		int num = ioIndex;
		value = ioBuffer[num++];
		if ((value & 0x80) == 0)
		{
			return 1;
		}
		value &= 127u;
		if (available == 1)
		{
			throw EoF(this);
		}
		uint num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 7;
		if ((num2 & 0x80) == 0)
		{
			return 2;
		}
		if (available == 2)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 14;
		if ((num2 & 0x80) == 0)
		{
			return 3;
		}
		if (available == 3)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 21;
		if ((num2 & 0x80) == 0)
		{
			return 4;
		}
		if (available == 4)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num];
		value |= num2 << 28;
		if ((num2 & 0xF0) == 0)
		{
			return 5;
		}
		if (trimNegative && (num2 & 0xF0) == 240 && available >= 10 && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == 1)
		{
			return 10;
		}
		throw AddErrorData(new OverflowException(), this);
	}

	private uint ReadUInt32Variant(bool trimNegative)
	{
		uint value;
		int num = TryReadUInt32VariantWithoutMoving(trimNegative, out value);
		if (num > 0)
		{
			ioIndex += num;
			available -= num;
			LongPosition += num;
			return value;
		}
		throw EoF(this);
	}

	private bool TryReadUInt32Variant(out uint value)
	{
		int num = TryReadUInt32VariantWithoutMoving(trimNegative: false, out value);
		if (num > 0)
		{
			ioIndex += num;
			available -= num;
			LongPosition += num;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Reads an unsigned 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public uint ReadUInt32()
	{
		switch (WireType)
		{
		case WireType.Variant:
			return ReadUInt32Variant(trimNegative: false);
		case WireType.Fixed32:
			if (available < 4)
			{
				Ensure(4, strict: true);
			}
			LongPosition += 4L;
			available -= 4;
			return (uint)(ioBuffer[ioIndex++] | (ioBuffer[ioIndex++] << 8) | (ioBuffer[ioIndex++] << 16) | (ioBuffer[ioIndex++] << 24));
		case WireType.Fixed64:
			return checked((uint)ReadUInt64());
		default:
			throw CreateWireTypeException();
		}
	}

	internal void Ensure(int count, bool strict)
	{
		if (count > ioBuffer.Length)
		{
			BufferPool.ResizeAndFlushLeft(ref ioBuffer, count, ioIndex, available);
			ioIndex = 0;
		}
		else if (ioIndex + count >= ioBuffer.Length)
		{
			Buffer.BlockCopy(ioBuffer, ioIndex, ioBuffer, 0, available);
			ioIndex = 0;
		}
		count -= available;
		int num = ioIndex + available;
		int num2 = ioBuffer.Length - num;
		if (isFixedLength && dataRemaining64 < num2)
		{
			num2 = (int)dataRemaining64;
		}
		int num3;
		while (count > 0 && num2 > 0 && (num3 = source.Read(ioBuffer, num, num2)) > 0)
		{
			available += num3;
			count -= num3;
			num2 -= num3;
			num += num3;
			if (isFixedLength)
			{
				dataRemaining64 -= num3;
			}
		}
		if (strict && count > 0)
		{
			throw EoF(this);
		}
	}

	/// <summary>
	/// Reads a signed 16-bit integer from the stream: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public short ReadInt16()
	{
		return checked((short)ReadInt32());
	}

	/// <summary>
	/// Reads an unsigned 16-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public ushort ReadUInt16()
	{
		return checked((ushort)ReadUInt32());
	}

	/// <summary>
	/// Reads an unsigned 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public byte ReadByte()
	{
		return checked((byte)ReadUInt32());
	}

	/// <summary>
	/// Reads a signed 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public sbyte ReadSByte()
	{
		return checked((sbyte)ReadInt32());
	}

	/// <summary>
	/// Reads a signed 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public int ReadInt32()
	{
		switch (WireType)
		{
		case WireType.Variant:
			return (int)ReadUInt32Variant(trimNegative: true);
		case WireType.Fixed32:
			if (available < 4)
			{
				Ensure(4, strict: true);
			}
			LongPosition += 4L;
			available -= 4;
			return ioBuffer[ioIndex++] | (ioBuffer[ioIndex++] << 8) | (ioBuffer[ioIndex++] << 16) | (ioBuffer[ioIndex++] << 24);
		case WireType.Fixed64:
			return checked((int)ReadInt64());
		case WireType.SignedVariant:
			return Zag(ReadUInt32Variant(trimNegative: true));
		default:
			throw CreateWireTypeException();
		}
	}

	private static int Zag(uint ziggedValue)
	{
		return (int)(0 - (ziggedValue & 1)) ^ (((int)ziggedValue >> 1) & 0x7FFFFFFF);
	}

	private static long Zag(ulong ziggedValue)
	{
		return (long)(0L - (ziggedValue & 1)) ^ (((long)ziggedValue >> 1) & 0x7FFFFFFFFFFFFFFFL);
	}

	/// <summary>
	/// Reads a signed 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
	/// </summary>
	public long ReadInt64()
	{
		switch (WireType)
		{
		case WireType.Variant:
			return (long)ReadUInt64Variant();
		case WireType.Fixed32:
			return ReadInt32();
		case WireType.Fixed64:
			if (available < 8)
			{
				Ensure(8, strict: true);
			}
			LongPosition += 8L;
			available -= 8;
			return (long)(ioBuffer[ioIndex++] | ((ulong)ioBuffer[ioIndex++] << 8) | ((ulong)ioBuffer[ioIndex++] << 16) | ((ulong)ioBuffer[ioIndex++] << 24) | ((ulong)ioBuffer[ioIndex++] << 32) | ((ulong)ioBuffer[ioIndex++] << 40) | ((ulong)ioBuffer[ioIndex++] << 48) | ((ulong)ioBuffer[ioIndex++] << 56));
		case WireType.SignedVariant:
			return Zag(ReadUInt64Variant());
		default:
			throw CreateWireTypeException();
		}
	}

	private int TryReadUInt64VariantWithoutMoving(out ulong value)
	{
		if (available < 10)
		{
			Ensure(10, strict: false);
		}
		if (available == 0)
		{
			value = 0uL;
			return 0;
		}
		int num = ioIndex;
		value = ioBuffer[num++];
		if ((value & 0x80) == 0L)
		{
			return 1;
		}
		value &= 127uL;
		if (available == 1)
		{
			throw EoF(this);
		}
		ulong num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 7;
		if ((num2 & 0x80) == 0L)
		{
			return 2;
		}
		if (available == 2)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 14;
		if ((num2 & 0x80) == 0L)
		{
			return 3;
		}
		if (available == 3)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 21;
		if ((num2 & 0x80) == 0L)
		{
			return 4;
		}
		if (available == 4)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 28;
		if ((num2 & 0x80) == 0L)
		{
			return 5;
		}
		if (available == 5)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 35;
		if ((num2 & 0x80) == 0L)
		{
			return 6;
		}
		if (available == 6)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 42;
		if ((num2 & 0x80) == 0L)
		{
			return 7;
		}
		if (available == 7)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 49;
		if ((num2 & 0x80) == 0L)
		{
			return 8;
		}
		if (available == 8)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num++];
		value |= (num2 & 0x7F) << 56;
		if ((num2 & 0x80) == 0L)
		{
			return 9;
		}
		if (available == 9)
		{
			throw EoF(this);
		}
		num2 = ioBuffer[num];
		value |= num2 << 63;
		if ((num2 & 0xFFFFFFFFFFFFFFFEuL) != 0L)
		{
			throw AddErrorData(new OverflowException(), this);
		}
		return 10;
	}

	private ulong ReadUInt64Variant()
	{
		ulong value;
		int num = TryReadUInt64VariantWithoutMoving(out value);
		if (num > 0)
		{
			ioIndex += num;
			available -= num;
			LongPosition += num;
			return value;
		}
		throw EoF(this);
	}

	private string Intern(string value)
	{
		if (value == null)
		{
			return null;
		}
		if (value.Length == 0)
		{
			return "";
		}
		string value2;
		if (stringInterner == null)
		{
			stringInterner = new Dictionary<string, string> { { value, value } };
		}
		else if (stringInterner.TryGetValue(value, out value2))
		{
			value = value2;
		}
		else
		{
			stringInterner.Add(value, value);
		}
		return value;
	}

	/// <summary>
	/// Reads a string from the stream (using UTF8); supported wire-types: String
	/// </summary>
	public string ReadString()
	{
		if (WireType == WireType.String)
		{
			int num = (int)ReadUInt32Variant(trimNegative: false);
			if (num == 0)
			{
				return "";
			}
			if (num < 0)
			{
				ThrowInvalidLength(num);
			}
			if (available < num)
			{
				Ensure(num, strict: true);
			}
			string text = encoding.GetString(ioBuffer, ioIndex, num);
			if (InternStrings)
			{
				text = Intern(text);
			}
			available -= num;
			LongPosition += num;
			ioIndex += num;
			return text;
		}
		throw CreateWireTypeException();
	}

	/// <summary>
	/// Throws an exception indication that the given value cannot be mapped to an enum.
	/// </summary>
	public void ThrowEnumException(Type type, int value)
	{
		string text = ((type == null) ? "<null>" : type.FullName);
		throw AddErrorData(new ProtoException("No " + text + " enum is mapped to the wire-value " + value), this);
	}

	private void ThrowInvalidLength(long length)
	{
		throw AddErrorData(new InvalidOperationException("Invalid length: " + length), this);
	}

	private Exception CreateWireTypeException()
	{
		return CreateException("Invalid wire-type; this usually means you have over-written a file without truncating or setting the length; see https://stackoverflow.com/q/2152978/23354");
	}

	private Exception CreateException(string message)
	{
		return AddErrorData(new ProtoException(message), this);
	}

	/// <summary>
	/// Reads a double-precision number from the stream; supported wire-types: Fixed32, Fixed64
	/// </summary>
	public unsafe double ReadDouble()
	{
		switch (WireType)
		{
		case WireType.Fixed32:
			return ReadSingle();
		case WireType.Fixed64:
		{
			long num = ReadInt64();
			return *(double*)(&num);
		}
		default:
			throw CreateWireTypeException();
		}
	}

	/// <summary>
	/// Reads (merges) a sub-message from the stream, internally calling StartSubItem and EndSubItem, and (in between)
	/// parsing the message in accordance with the model associated with the reader
	/// </summary>
	public static object ReadObject(object value, int key, ProtoReader reader)
	{
		return ReadTypedObject(value, key, reader, null);
	}

	internal static object ReadTypedObject(object value, int key, ProtoReader reader, Type type)
	{
		if (reader.Model == null)
		{
			throw AddErrorData(new InvalidOperationException("Cannot deserialize sub-objects unless a model is provided"), reader);
		}
		SubItemToken token = StartSubItem(reader);
		if (key >= 0)
		{
			value = reader.Model.Deserialize(key, value, reader);
		}
		else if (!(type != null) || !reader.Model.TryDeserializeAuxiliaryType(reader, DataFormat.Default, 1, type, ref value, skipOtherFields: true, asListItem: false, autoCreate: true, insideList: false, null))
		{
			TypeModel.ThrowUnexpectedType(type);
		}
		EndSubItem(token, reader);
		return value;
	}

	/// <summary>
	/// Makes the end of consuming a nested message in the stream; the stream must be either at the correct EndGroup
	/// marker, or all fields of the sub-message must have been consumed (in either case, this means ReadFieldHeader
	/// should return zero)
	/// </summary>
	public static void EndSubItem(SubItemToken token, ProtoReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		long value = token.value64;
		if (reader.WireType == WireType.EndGroup)
		{
			if (value >= 0)
			{
				throw AddErrorData(new ArgumentException("token"), reader);
			}
			if (-(int)value != reader.FieldNumber)
			{
				throw reader.CreateException("Wrong group was ended");
			}
			reader.WireType = WireType.None;
			reader.depth--;
			return;
		}
		if (value < reader.LongPosition)
		{
			throw reader.CreateException($"Sub-message not read entirely; expected {value}, was {reader.LongPosition}");
		}
		if (reader.blockEnd64 != reader.LongPosition && reader.blockEnd64 != long.MaxValue)
		{
			throw reader.CreateException("Sub-message not read correctly");
		}
		reader.blockEnd64 = value;
		reader.depth--;
	}

	/// <summary>
	/// Begins consuming a nested message in the stream; supported wire-types: StartGroup, String
	/// </summary>
	/// <remarks>The token returned must be help and used when callining EndSubItem</remarks>
	public static SubItemToken StartSubItem(ProtoReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		switch (reader.WireType)
		{
		case WireType.StartGroup:
			reader.WireType = WireType.None;
			reader.depth++;
			return new SubItemToken((long)(-reader.FieldNumber));
		case WireType.String:
		{
			long num = (long)reader.ReadUInt64Variant();
			if (num < 0)
			{
				reader.ThrowInvalidLength(num);
			}
			long value = reader.blockEnd64;
			reader.blockEnd64 = reader.LongPosition + num;
			reader.depth++;
			return new SubItemToken(value);
		}
		default:
			throw reader.CreateWireTypeException();
		}
	}

	/// <summary>
	/// Reads a field header from the stream, setting the wire-type and retuning the field number. If no
	/// more fields are available, then 0 is returned. This methods respects sub-messages.
	/// </summary>
	public int ReadFieldHeader()
	{
		if (blockEnd64 <= LongPosition || WireType == WireType.EndGroup)
		{
			return 0;
		}
		if (TryReadUInt32Variant(out var value) && value != 0)
		{
			WireType = (WireType)(value & 7);
			FieldNumber = (int)(value >> 3);
			if (FieldNumber < 1)
			{
				throw new ProtoException("Invalid field in source data: " + FieldNumber);
			}
		}
		else
		{
			WireType = WireType.None;
			FieldNumber = 0;
		}
		if (WireType == WireType.EndGroup)
		{
			if (depth > 0)
			{
				return 0;
			}
			throw new ProtoException("Unexpected end-group in source data; this usually means the source data is corrupt");
		}
		return FieldNumber;
	}

	/// <summary>
	/// Looks ahead to see whether the next field in the stream is what we expect
	/// (typically; what we've just finished reading - for example ot read successive list items)
	/// </summary>
	public bool TryReadFieldHeader(int field)
	{
		if (blockEnd64 <= LongPosition || WireType == WireType.EndGroup)
		{
			return false;
		}
		uint value;
		int num = TryReadUInt32VariantWithoutMoving(trimNegative: false, out value);
		WireType wireType;
		if (num > 0 && (int)value >> 3 == field && (wireType = (WireType)(value & 7)) != WireType.EndGroup)
		{
			WireType = wireType;
			FieldNumber = field;
			LongPosition += num;
			ioIndex += num;
			available -= num;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Compares the streams current wire-type to the hinted wire-type, updating the reader if necessary; for example,
	/// a Variant may be updated to SignedVariant. If the hinted wire-type is unrelated then no change is made.
	/// </summary>
	public void Hint(WireType wireType)
	{
		if (WireType != wireType && (wireType & (WireType)7) == WireType)
		{
			WireType = wireType;
		}
	}

	/// <summary>
	/// Verifies that the stream's current wire-type is as expected, or a specialized sub-type (for example,
	/// SignedVariant) - in which case the current wire-type is updated. Otherwise an exception is thrown.
	/// </summary>
	public void Assert(WireType wireType)
	{
		if (WireType != wireType)
		{
			if ((wireType & (WireType)7) != WireType)
			{
				throw CreateWireTypeException();
			}
			WireType = wireType;
		}
	}

	/// <summary>
	/// Discards the data for the current field.
	/// </summary>
	public void SkipField()
	{
		switch (WireType)
		{
		case WireType.Fixed32:
			if (available < 4)
			{
				Ensure(4, strict: true);
			}
			available -= 4;
			ioIndex += 4;
			LongPosition += 4L;
			break;
		case WireType.Fixed64:
			if (available < 8)
			{
				Ensure(8, strict: true);
			}
			available -= 8;
			ioIndex += 8;
			LongPosition += 8L;
			break;
		case WireType.String:
		{
			long num = (long)ReadUInt64Variant();
			if (num < 0)
			{
				ThrowInvalidLength(num);
			}
			if (num <= available)
			{
				available -= (int)num;
				ioIndex += (int)num;
				LongPosition += num;
				break;
			}
			LongPosition += num;
			num -= available;
			ioIndex = (available = 0);
			if (isFixedLength)
			{
				if (num > dataRemaining64)
				{
					throw EoF(this);
				}
				dataRemaining64 -= num;
			}
			Seek(source, num, ioBuffer);
			break;
		}
		case WireType.Variant:
		case WireType.SignedVariant:
			ReadUInt64Variant();
			break;
		case WireType.StartGroup:
		{
			int fieldNumber = FieldNumber;
			depth++;
			while (ReadFieldHeader() > 0)
			{
				SkipField();
			}
			depth--;
			if (WireType == WireType.EndGroup && FieldNumber == fieldNumber)
			{
				WireType = WireType.None;
				break;
			}
			throw CreateWireTypeException();
		}
		default:
			throw CreateWireTypeException();
		}
	}

	/// <summary>
	/// Reads an unsigned 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	public ulong ReadUInt64()
	{
		switch (WireType)
		{
		case WireType.Variant:
			return ReadUInt64Variant();
		case WireType.Fixed32:
			return ReadUInt32();
		case WireType.Fixed64:
			if (available < 8)
			{
				Ensure(8, strict: true);
			}
			LongPosition += 8L;
			available -= 8;
			return ioBuffer[ioIndex++] | ((ulong)ioBuffer[ioIndex++] << 8) | ((ulong)ioBuffer[ioIndex++] << 16) | ((ulong)ioBuffer[ioIndex++] << 24) | ((ulong)ioBuffer[ioIndex++] << 32) | ((ulong)ioBuffer[ioIndex++] << 40) | ((ulong)ioBuffer[ioIndex++] << 48) | ((ulong)ioBuffer[ioIndex++] << 56);
		default:
			throw CreateWireTypeException();
		}
	}

	/// <summary>
	/// Reads a single-precision number from the stream; supported wire-types: Fixed32, Fixed64
	/// </summary>
	public unsafe float ReadSingle()
	{
		switch (WireType)
		{
		case WireType.Fixed32:
		{
			int num3 = ReadInt32();
			return *(float*)(&num3);
		}
		case WireType.Fixed64:
		{
			double num = ReadDouble();
			float num2 = (float)num;
			if (float.IsInfinity(num2) && !double.IsInfinity(num))
			{
				throw AddErrorData(new OverflowException(), this);
			}
			return num2;
		}
		default:
			throw CreateWireTypeException();
		}
	}

	/// <summary>
	/// Reads a boolean value from the stream; supported wire-types: Variant, Fixed32, Fixed64
	/// </summary>
	/// <returns></returns>
	public bool ReadBoolean()
	{
		return ReadUInt32() switch
		{
			0u => false, 
			1u => true, 
			_ => throw CreateException("Unexpected boolean value"), 
		};
	}

	/// <summary>
	/// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
	/// </summary>
	public static byte[] AppendBytes(byte[] value, ProtoReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		switch (reader.WireType)
		{
		case WireType.String:
		{
			int num = (int)reader.ReadUInt32Variant(trimNegative: false);
			reader.WireType = WireType.None;
			if (num == 0)
			{
				return value ?? EmptyBlob;
			}
			if (num < 0)
			{
				reader.ThrowInvalidLength(num);
			}
			int num2;
			if (value == null || value.Length == 0)
			{
				num2 = 0;
				value = new byte[num];
			}
			else
			{
				num2 = value.Length;
				byte[] array = new byte[value.Length + num];
				Buffer.BlockCopy(value, 0, array, 0, value.Length);
				value = array;
			}
			reader.LongPosition += num;
			while (num > reader.available)
			{
				if (reader.available > 0)
				{
					Buffer.BlockCopy(reader.ioBuffer, reader.ioIndex, value, num2, reader.available);
					num -= reader.available;
					num2 += reader.available;
					reader.ioIndex = (reader.available = 0);
				}
				int num3 = ((num > reader.ioBuffer.Length) ? reader.ioBuffer.Length : num);
				if (num3 > 0)
				{
					reader.Ensure(num3, strict: true);
				}
			}
			if (num > 0)
			{
				Buffer.BlockCopy(reader.ioBuffer, reader.ioIndex, value, num2, num);
				reader.ioIndex += num;
				reader.available -= num;
			}
			return value;
		}
		case WireType.Variant:
			return new byte[0];
		default:
			throw reader.CreateWireTypeException();
		}
	}

	private static int ReadByteOrThrow(Stream source)
	{
		int num = source.ReadByte();
		if (num < 0)
		{
			throw EoF(null);
		}
		return num;
	}

	/// <summary>
	/// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
	/// reader to be created.
	/// </summary>
	public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber)
	{
		int bytesRead;
		return ReadLengthPrefix(source, expectHeader, style, out fieldNumber, out bytesRead);
	}

	/// <summary>
	/// Reads a little-endian encoded integer. An exception is thrown if the data is not all available.
	/// </summary>
	public static int DirectReadLittleEndianInt32(Stream source)
	{
		return ReadByteOrThrow(source) | (ReadByteOrThrow(source) << 8) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 24);
	}

	/// <summary>
	/// Reads a big-endian encoded integer. An exception is thrown if the data is not all available.
	/// </summary>
	public static int DirectReadBigEndianInt32(Stream source)
	{
		return (ReadByteOrThrow(source) << 24) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 8) | ReadByteOrThrow(source);
	}

	/// <summary>
	/// Reads a varint encoded integer. An exception is thrown if the data is not all available.
	/// </summary>
	public static int DirectReadVarintInt32(Stream source)
	{
		if (TryReadUInt64Variant(source, out var value) <= 0)
		{
			throw EoF(null);
		}
		return checked((int)value);
	}

	/// <summary>
	/// Reads a string (of a given lenth, in bytes) directly from the source into a pre-existing buffer. An exception is thrown if the data is not all available.
	/// </summary>
	public static void DirectReadBytes(Stream source, byte[] buffer, int offset, int count)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		int num;
		while (count > 0 && (num = source.Read(buffer, offset, count)) > 0)
		{
			count -= num;
			offset += num;
		}
		if (count > 0)
		{
			throw EoF(null);
		}
	}

	/// <summary>
	/// Reads a given number of bytes directly from the source. An exception is thrown if the data is not all available.
	/// </summary>
	public static byte[] DirectReadBytes(Stream source, int count)
	{
		byte[] array = new byte[count];
		DirectReadBytes(source, array, 0, count);
		return array;
	}

	/// <summary>
	/// Reads a string (of a given lenth, in bytes) directly from the source. An exception is thrown if the data is not all available.
	/// </summary>
	public static string DirectReadString(Stream source, int length)
	{
		byte[] array = new byte[length];
		DirectReadBytes(source, array, 0, length);
		return Encoding.UTF8.GetString(array, 0, length);
	}

	/// <summary>
	/// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
	/// reader to be created.
	/// </summary>
	public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber, out int bytesRead)
	{
		if (style == PrefixStyle.None)
		{
			bytesRead = (fieldNumber = 0);
			return int.MaxValue;
		}
		return checked((int)ReadLongLengthPrefix(source, expectHeader, style, out fieldNumber, out bytesRead));
	}

	/// <summary>
	/// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
	/// reader to be created.
	/// </summary>
	public static long ReadLongLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber, out int bytesRead)
	{
		fieldNumber = 0;
		switch (style)
		{
		case PrefixStyle.None:
			bytesRead = 0;
			return long.MaxValue;
		case PrefixStyle.Base128:
		{
			bytesRead = 0;
			ulong value;
			int num2;
			if (expectHeader)
			{
				num2 = TryReadUInt64Variant(source, out value);
				bytesRead += num2;
				if (num2 > 0)
				{
					if ((value & 7) != 2)
					{
						throw new InvalidOperationException();
					}
					fieldNumber = (int)(value >> 3);
					num2 = TryReadUInt64Variant(source, out value);
					bytesRead += num2;
					if (bytesRead == 0)
					{
						throw EoF(null);
					}
					return (long)value;
				}
				bytesRead = 0;
				return -1L;
			}
			num2 = TryReadUInt64Variant(source, out value);
			bytesRead += num2;
			if (bytesRead >= 0)
			{
				return (long)value;
			}
			return -1L;
		}
		case PrefixStyle.Fixed32:
		{
			int num3 = source.ReadByte();
			if (num3 < 0)
			{
				bytesRead = 0;
				return -1L;
			}
			bytesRead = 4;
			return num3 | (ReadByteOrThrow(source) << 8) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 24);
		}
		case PrefixStyle.Fixed32BigEndian:
		{
			int num = source.ReadByte();
			if (num < 0)
			{
				bytesRead = 0;
				return -1L;
			}
			bytesRead = 4;
			return (num << 24) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 8) | ReadByteOrThrow(source);
		}
		default:
			throw new ArgumentOutOfRangeException("style");
		}
	}

	/// <returns>The number of bytes consumed; 0 if no data available</returns>
	private static int TryReadUInt64Variant(Stream source, out ulong value)
	{
		value = 0uL;
		int num = source.ReadByte();
		if (num < 0)
		{
			return 0;
		}
		value = (uint)num;
		if ((value & 0x80) == 0L)
		{
			return 1;
		}
		value &= 127uL;
		int num2 = 1;
		int num3 = 7;
		while (num2 < 9)
		{
			num = source.ReadByte();
			if (num < 0)
			{
				throw EoF(null);
			}
			value |= ((ulong)num & 0x7FuL) << num3;
			num3 += 7;
			num2++;
			if ((num & 0x80) == 0)
			{
				return num2;
			}
		}
		num = source.ReadByte();
		if (num < 0)
		{
			throw EoF(null);
		}
		if ((num & 1) == 0)
		{
			value |= ((ulong)num & 0x7FuL) << num3;
			return ++num2;
		}
		throw new OverflowException();
	}

	internal static void Seek(Stream source, long count, byte[] buffer)
	{
		if (source.CanSeek)
		{
			source.Seek(count, SeekOrigin.Current);
			count = 0L;
		}
		else if (buffer != null)
		{
			int num;
			while (count > buffer.Length && (num = source.Read(buffer, 0, buffer.Length)) > 0)
			{
				count -= num;
			}
			while (count > 0 && (num = source.Read(buffer, 0, (int)count)) > 0)
			{
				count -= num;
			}
		}
		else
		{
			buffer = BufferPool.GetBuffer();
			try
			{
				int num2;
				while (count > buffer.Length && (num2 = source.Read(buffer, 0, buffer.Length)) > 0)
				{
					count -= num2;
				}
				while (count > 0 && (num2 = source.Read(buffer, 0, (int)count)) > 0)
				{
					count -= num2;
				}
			}
			finally
			{
				BufferPool.ReleaseBufferToPool(ref buffer);
			}
		}
		if (count > 0)
		{
			throw EoF(null);
		}
	}

	internal static Exception AddErrorData(Exception exception, ProtoReader source)
	{
		if (exception != null && source != null && !exception.Data.Contains("protoSource"))
		{
			exception.Data.Add("protoSource", $"tag={source.FieldNumber}; wire-type={source.WireType}; offset={source.LongPosition}; depth={source.depth}");
		}
		return exception;
	}

	private static Exception EoF(ProtoReader source)
	{
		return AddErrorData(new EndOfStreamException(), source);
	}

	/// <summary>
	/// Copies the current field into the instance as extension data
	/// </summary>
	public void AppendExtensionData(IExtensible instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		IExtension extensionObject = instance.GetExtensionObject(createIfMissing: true);
		bool commit = false;
		Stream stream = extensionObject.BeginAppend();
		try
		{
			using (ProtoWriter protoWriter = ProtoWriter.Create(stream, Model))
			{
				AppendExtensionField(protoWriter);
				protoWriter.Close();
			}
			commit = true;
		}
		finally
		{
			extensionObject.EndAppend(stream, commit);
		}
	}

	private void AppendExtensionField(ProtoWriter writer)
	{
		ProtoWriter.WriteFieldHeader(FieldNumber, WireType, writer);
		switch (WireType)
		{
		case WireType.Fixed32:
			ProtoWriter.WriteInt32(ReadInt32(), writer);
			break;
		case WireType.Variant:
		case WireType.Fixed64:
		case WireType.SignedVariant:
			ProtoWriter.WriteInt64(ReadInt64(), writer);
			break;
		case WireType.String:
			ProtoWriter.WriteBytes(AppendBytes(null, this), writer);
			break;
		case WireType.StartGroup:
		{
			SubItemToken token = StartSubItem(this);
			SubItemToken token2 = ProtoWriter.StartSubItem(null, writer);
			while (ReadFieldHeader() > 0)
			{
				AppendExtensionField(writer);
			}
			EndSubItem(token, this);
			ProtoWriter.EndSubItem(token2, writer);
			break;
		}
		default:
			throw CreateWireTypeException();
		}
	}

	/// <summary>
	/// Indicates whether the reader still has data remaining in the current sub-item,
	/// additionally setting the wire-type for the next field if there is more data.
	/// This is used when decoding packed data.
	/// </summary>
	public static bool HasSubValue(WireType wireType, ProtoReader source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (source.blockEnd64 <= source.LongPosition || wireType == WireType.EndGroup)
		{
			return false;
		}
		source.WireType = wireType;
		return true;
	}

	internal int GetTypeKey(ref Type type)
	{
		return Model.GetKey(ref type);
	}

	internal Type DeserializeType(string value)
	{
		return TypeModel.DeserializeType(Model, value);
	}

	internal void SetRootObject(object value)
	{
		NetCache.SetKeyedObject(0, value);
		trapCount--;
	}

	/// <summary>
	/// Utility method, not intended for public use; this helps maintain the root object is complex scenarios
	/// </summary>
	public static void NoteObject(object value, ProtoReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.trapCount != 0)
		{
			reader.NetCache.RegisterTrappedObject(value);
			reader.trapCount--;
		}
	}

	/// <summary>
	/// Reads a Type from the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
	/// </summary>
	public Type ReadType()
	{
		return TypeModel.DeserializeType(Model, ReadString());
	}

	internal void TrapNextObject(int newObjectKey)
	{
		trapCount++;
		NetCache.SetKeyedObject(newObjectKey, null);
	}

	internal void CheckFullyConsumed()
	{
		if (isFixedLength)
		{
			if (dataRemaining64 != 0L)
			{
				throw new ProtoException("Incorrect number of bytes consumed");
			}
		}
		else if (available != 0)
		{
			throw new ProtoException("Unconsumed data left in the buffer; this suggests corrupt input");
		}
	}

	/// <summary>
	/// Merge two objects using the details from the current reader; this is used to change the type
	/// of objects when an inheritance relationship is discovered later than usual during deserilazation.
	/// </summary>
	public static object Merge(ProtoReader parent, object from, object to)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		TypeModel model = parent.Model;
		SerializationContext context = parent.Context;
		if (model == null)
		{
			throw new InvalidOperationException("Types cannot be merged unless a type-model has been specified");
		}
		using MemoryStream memoryStream = new MemoryStream();
		model.Serialize(memoryStream, from, context);
		memoryStream.Position = 0L;
		return model.Deserialize(memoryStream, to, null);
	}

	internal static ProtoReader Create(Stream source, TypeModel model, SerializationContext context, int len)
	{
		return Create(source, model, context, (long)len);
	}

	/// <summary>
	/// Creates a new reader against a stream
	/// </summary>
	/// <param name="source">The source stream</param>
	/// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
	/// <param name="context">Additional context about this serialization operation</param>
	/// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
	public static ProtoReader Create(Stream source, TypeModel model, SerializationContext context = null, long length = -1L)
	{
		ProtoReader recycled = GetRecycled();
		if (recycled == null)
		{
			return new ProtoReader(source, model, context, length);
		}
		Init(recycled, source, model, context, length);
		return recycled;
	}

	private static ProtoReader GetRecycled()
	{
		ProtoReader result = lastReader;
		lastReader = null;
		return result;
	}

	internal static void Recycle(ProtoReader reader)
	{
		if (reader != null)
		{
			reader.Dispose();
			lastReader = reader;
		}
	}
}
