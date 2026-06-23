using System;

namespace ProtoBuf.Serializers;

internal sealed class EnumSerializer : IProtoSerializer
{
	public readonly struct EnumPair
	{
		public readonly object RawValue;

		public readonly Enum TypedValue;

		public readonly int WireValue;

		public EnumPair(int wireValue, object raw, Type type)
		{
			WireValue = wireValue;
			RawValue = raw;
			TypedValue = (Enum)Enum.ToObject(type, raw);
		}
	}

	private readonly EnumPair[] map;

	public Type ExpectedType { get; }

	bool IProtoSerializer.RequiresOldValue => false;

	bool IProtoSerializer.ReturnsValue => true;

	public EnumSerializer(Type enumType, EnumPair[] map)
	{
		ExpectedType = enumType ?? throw new ArgumentNullException("enumType");
		this.map = map;
		if (map == null)
		{
			return;
		}
		for (int i = 1; i < map.Length; i++)
		{
			for (int j = 0; j < i; j++)
			{
				if (map[i].WireValue == map[j].WireValue && !object.Equals(map[i].RawValue, map[j].RawValue))
				{
					throw new ProtoException("Multiple enums with wire-value " + map[i].WireValue);
				}
				if (object.Equals(map[i].RawValue, map[j].RawValue) && map[i].WireValue != map[j].WireValue)
				{
					throw new ProtoException("Multiple enums with deserialized-value " + map[i].RawValue);
				}
			}
		}
	}

	private ProtoTypeCode GetTypeCode()
	{
		Type type = Helpers.GetUnderlyingType(ExpectedType);
		if (type == null)
		{
			type = ExpectedType;
		}
		return Helpers.GetTypeCode(type);
	}

	private int EnumToWire(object value)
	{
		return GetTypeCode() switch
		{
			ProtoTypeCode.Byte => (byte)value, 
			ProtoTypeCode.SByte => (sbyte)value, 
			ProtoTypeCode.Int16 => (short)value, 
			ProtoTypeCode.Int32 => (int)value, 
			ProtoTypeCode.Int64 => (int)(long)value, 
			ProtoTypeCode.UInt16 => (ushort)value, 
			ProtoTypeCode.UInt32 => (int)(uint)value, 
			ProtoTypeCode.UInt64 => (int)(ulong)value, 
			_ => throw new InvalidOperationException(), 
		};
	}

	private object WireToEnum(int value)
	{
		return GetTypeCode() switch
		{
			ProtoTypeCode.Byte => Enum.ToObject(ExpectedType, (byte)value), 
			ProtoTypeCode.SByte => Enum.ToObject(ExpectedType, (sbyte)value), 
			ProtoTypeCode.Int16 => Enum.ToObject(ExpectedType, (short)value), 
			ProtoTypeCode.Int32 => Enum.ToObject(ExpectedType, value), 
			ProtoTypeCode.Int64 => Enum.ToObject(ExpectedType, (long)value), 
			ProtoTypeCode.UInt16 => Enum.ToObject(ExpectedType, (ushort)value), 
			ProtoTypeCode.UInt32 => Enum.ToObject(ExpectedType, (uint)value), 
			ProtoTypeCode.UInt64 => Enum.ToObject(ExpectedType, (ulong)value), 
			_ => throw new InvalidOperationException(), 
		};
	}

	public object Read(object value, ProtoReader source)
	{
		int num = source.ReadInt32();
		if (map == null)
		{
			return WireToEnum(num);
		}
		for (int i = 0; i < map.Length; i++)
		{
			if (map[i].WireValue == num)
			{
				return map[i].TypedValue;
			}
		}
		source.ThrowEnumException(ExpectedType, num);
		return null;
	}

	public void Write(object value, ProtoWriter dest)
	{
		if (map == null)
		{
			ProtoWriter.WriteInt32(EnumToWire(value), dest);
			return;
		}
		for (int i = 0; i < map.Length; i++)
		{
			if (object.Equals(map[i].TypedValue, value))
			{
				ProtoWriter.WriteInt32(map[i].WireValue, dest);
				return;
			}
		}
		ProtoWriter.ThrowEnumException(dest, value);
	}
}
