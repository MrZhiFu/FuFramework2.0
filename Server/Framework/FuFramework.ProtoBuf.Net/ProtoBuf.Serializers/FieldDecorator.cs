using System;
using System.Reflection;

namespace ProtoBuf.Serializers;

internal sealed class FieldDecorator : ProtoDecoratorBase
{
	private readonly FieldInfo field;

	public override Type ExpectedType { get; }

	public override bool RequiresOldValue => true;

	public override bool ReturnsValue => false;

	public FieldDecorator(Type forType, FieldInfo field, IProtoSerializer tail)
		: base(tail)
	{
		ExpectedType = forType;
		this.field = field;
	}

	public override void Write(object value, ProtoWriter dest)
	{
		value = field.GetValue(value);
		if (value != null)
		{
			Tail.Write(value, dest);
		}
	}

	public override object Read(object value, ProtoReader source)
	{
		object obj = Tail.Read(Tail.RequiresOldValue ? field.GetValue(value) : null, source);
		if (obj != null)
		{
			field.SetValue(value, obj);
		}
		return null;
	}
}
