using System;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers;

internal sealed class NetObjectSerializer : IProtoSerializer
{
	private readonly int key;

	private readonly BclHelpers.NetObjectOptions options;

	public Type ExpectedType { get; }

	public bool ReturnsValue => true;

	public bool RequiresOldValue => true;

	public NetObjectSerializer(TypeModel model, Type type, int key, BclHelpers.NetObjectOptions options)
	{
		bool flag = (options & BclHelpers.NetObjectOptions.DynamicType) != 0;
		this.key = (flag ? (-1) : key);
		ExpectedType = (flag ? model.MapType(typeof(object)) : type);
		this.options = options;
	}

	public object Read(object value, ProtoReader source)
	{
		return BclHelpers.ReadNetObject(value, source, key, (ExpectedType == typeof(object)) ? null : ExpectedType, options);
	}

	public void Write(object value, ProtoWriter dest)
	{
		BclHelpers.WriteNetObject(value, dest, key, options);
	}
}
