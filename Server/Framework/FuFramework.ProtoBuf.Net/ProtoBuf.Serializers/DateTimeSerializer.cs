using System;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers;

internal sealed class DateTimeSerializer : IProtoSerializer
{
	private static readonly Type expectedType = typeof(DateTime);

	private readonly bool includeKind;

	private readonly bool wellKnown;

	public Type ExpectedType => expectedType;

	bool IProtoSerializer.RequiresOldValue => false;

	bool IProtoSerializer.ReturnsValue => true;

	public DateTimeSerializer(DataFormat dataFormat, TypeModel model)
	{
		wellKnown = dataFormat == DataFormat.WellKnown;
		includeKind = model?.SerializeDateTimeKind() ?? false;
	}

	public object Read(object value, ProtoReader source)
	{
		if (wellKnown)
		{
			return BclHelpers.ReadTimestamp(source);
		}
		return BclHelpers.ReadDateTime(source);
	}

	public void Write(object value, ProtoWriter dest)
	{
		if (wellKnown)
		{
			BclHelpers.WriteTimestamp((DateTime)value, dest);
		}
		else if (includeKind)
		{
			BclHelpers.WriteDateTimeWithKind((DateTime)value, dest);
		}
		else
		{
			BclHelpers.WriteDateTime((DateTime)value, dest);
		}
	}
}
