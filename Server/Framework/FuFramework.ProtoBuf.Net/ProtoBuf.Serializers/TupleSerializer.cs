using System;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers;

internal sealed class TupleSerializer : IProtoTypeSerializer, IProtoSerializer
{
	private readonly MemberInfo[] members;

	private readonly ConstructorInfo ctor;

	private readonly IProtoSerializer[] tails;

	public Type ExpectedType => ctor.DeclaringType;

	public bool RequiresOldValue => true;

	public bool ReturnsValue => false;

	public TupleSerializer(RuntimeTypeModel model, ConstructorInfo ctor, MemberInfo[] members)
	{
		this.ctor = ctor ?? throw new ArgumentNullException("ctor");
		this.members = members ?? throw new ArgumentNullException("members");
		tails = new IProtoSerializer[members.Length];
		ParameterInfo[] parameters = ctor.GetParameters();
		for (int i = 0; i < members.Length; i++)
		{
			Type parameterType = parameters[i].ParameterType;
			Type itemType = null;
			Type defaultType = null;
			MetaType.ResolveListTypes(model, parameterType, ref itemType, ref defaultType);
			Type type = ((itemType == null) ? parameterType : itemType);
			bool asReference = false;
			if (model.FindOrAddAuto(type, demand: false, addWithContractOnly: true) >= 0)
			{
				asReference = model[type].AsReferenceDefault;
			}
			IProtoSerializer protoSerializer = ValueMember.TryGetCoreSerializer(model, DataFormat.Default, type, out var defaultWireType, asReference, dynamicType: false, overwriteList: false, allowComplexTypes: true);
			if (protoSerializer == null)
			{
				throw new InvalidOperationException("No serializer defined for type: " + type.FullName);
			}
			protoSerializer = new TagDecorator(i + 1, defaultWireType, strict: false, protoSerializer);
			IProtoSerializer protoSerializer2 = ((!(itemType == null)) ? ((!parameterType.IsArray) ? ((ProtoDecoratorBase)ListDecorator.Create(model, parameterType, defaultType, protoSerializer, i + 1, writePacked: false, defaultWireType, returnList: true, overwriteList: false, supportNull: false)) : ((ProtoDecoratorBase)new ArrayDecorator(model, protoSerializer, i + 1, writePacked: false, defaultWireType, parameterType, overwriteList: false, supportNull: false))) : protoSerializer);
			tails[i] = protoSerializer2;
		}
	}

	public bool HasCallbacks(TypeModel.CallbackType callbackType)
	{
		return false;
	}

	void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
	{
	}

	object IProtoTypeSerializer.CreateInstance(ProtoReader source)
	{
		throw new NotSupportedException();
	}

	private object GetValue(object obj, int index)
	{
		PropertyInfo propertyInfo;
		if ((propertyInfo = members[index] as PropertyInfo) != null)
		{
			if (obj == null)
			{
				if (!Helpers.IsValueType(propertyInfo.PropertyType))
				{
					return null;
				}
				return Activator.CreateInstance(propertyInfo.PropertyType);
			}
			return propertyInfo.GetValue(obj, null);
		}
		FieldInfo fieldInfo;
		if ((fieldInfo = members[index] as FieldInfo) != null)
		{
			if (obj == null)
			{
				if (!Helpers.IsValueType(fieldInfo.FieldType))
				{
					return null;
				}
				return Activator.CreateInstance(fieldInfo.FieldType);
			}
			return fieldInfo.GetValue(obj);
		}
		throw new InvalidOperationException();
	}

	public object Read(object value, ProtoReader source)
	{
		object[] array = new object[members.Length];
		bool flag = false;
		if (value == null)
		{
			flag = true;
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = GetValue(value, i);
		}
		int num;
		while ((num = source.ReadFieldHeader()) > 0)
		{
			flag = true;
			if (num <= tails.Length)
			{
				IProtoSerializer protoSerializer = tails[num - 1];
				array[num - 1] = tails[num - 1].Read(protoSerializer.RequiresOldValue ? array[num - 1] : null, source);
			}
			else
			{
				source.SkipField();
			}
		}
		if (!flag)
		{
			return value;
		}
		return ctor.Invoke(array);
	}

	public void Write(object value, ProtoWriter dest)
	{
		for (int i = 0; i < tails.Length; i++)
		{
			object value2 = GetValue(value, i);
			if (value2 != null)
			{
				tails[i].Write(value2, dest);
			}
		}
	}

	private Type GetMemberType(int index)
	{
		Type memberType = Helpers.GetMemberType(members[index]);
		if (memberType == null)
		{
			throw new InvalidOperationException();
		}
		return memberType;
	}

	bool IProtoTypeSerializer.CanCreateInstance()
	{
		return false;
	}
}
