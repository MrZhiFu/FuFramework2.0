using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers;

internal sealed class ImmutableCollectionDecorator : ListDecorator
{
	private readonly MethodInfo builderFactory;

	private readonly MethodInfo add;

	private readonly MethodInfo addRange;

	private readonly MethodInfo finish;

	private readonly PropertyInfo isEmpty;

	private readonly PropertyInfo length;

	protected override bool RequireAdd => false;

	private static Type ResolveIReadOnlyCollection(Type declaredType, Type t)
	{
		if (CheckIsIReadOnlyCollectionExactly(declaredType))
		{
			return declaredType;
		}
		Type[] interfaces = declaredType.GetInterfaces();
		foreach (Type type in interfaces)
		{
			if (CheckIsIReadOnlyCollectionExactly(type))
			{
				return type;
			}
		}
		return null;
	}

	private static bool CheckIsIReadOnlyCollectionExactly(Type t)
	{
		if (t != null && t.IsGenericType && t.Name.StartsWith("IReadOnlyCollection`"))
		{
			Type[] genericArguments = t.GetGenericArguments();
			if (genericArguments.Length != 1 && genericArguments[0] != t)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	internal static bool IdentifyImmutable(TypeModel model, Type declaredType, out MethodInfo builderFactory, out PropertyInfo isEmpty, out PropertyInfo length, out MethodInfo add, out MethodInfo addRange, out MethodInfo finish)
	{
		builderFactory = (add = (addRange = (finish = null)));
		isEmpty = (length = null);
		if (model == null || declaredType == null)
		{
			return false;
		}
		if (!declaredType.IsGenericType)
		{
			return false;
		}
		Type[] genericArguments = declaredType.GetGenericArguments();
		Type[] array;
		switch (genericArguments.Length)
		{
		case 1:
			array = genericArguments;
			break;
		case 2:
		{
			Type type = model.MapType(typeof(KeyValuePair<, >));
			if (type == null)
			{
				return false;
			}
			type = type.MakeGenericType(genericArguments);
			array = new Type[1] { type };
			break;
		}
		default:
			return false;
		}
		if (ResolveIReadOnlyCollection(declaredType, null) == null)
		{
			return false;
		}
		string name = declaredType.Name;
		int num = name.IndexOf('`');
		if (num <= 0)
		{
			return false;
		}
		name = (declaredType.IsInterface ? name.Substring(1, num - 1) : name.Substring(0, num));
		Type type2 = model.GetType(declaredType.Namespace + "." + name, declaredType.Assembly);
		if (type2 == null && name == "ImmutableSet")
		{
			type2 = model.GetType(declaredType.Namespace + ".ImmutableHashSet", declaredType.Assembly);
		}
		if (type2 == null)
		{
			return false;
		}
		MethodInfo[] methods = type2.GetMethods();
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.IsStatic && !(methodInfo.Name != "CreateBuilder") && methodInfo.IsGenericMethodDefinition && methodInfo.GetParameters().Length == 0 && methodInfo.GetGenericArguments().Length == genericArguments.Length)
			{
				builderFactory = methodInfo.MakeGenericMethod(genericArguments);
				break;
			}
		}
		Type type3 = model.MapType(typeof(void));
		if (builderFactory == null || builderFactory.ReturnType == null || builderFactory.ReturnType == type3)
		{
			return false;
		}
		isEmpty = Helpers.GetProperty(declaredType, "IsDefaultOrEmpty", nonPublic: false);
		if (isEmpty == null)
		{
			isEmpty = Helpers.GetProperty(declaredType, "IsEmpty", nonPublic: false);
		}
		if (isEmpty == null)
		{
			length = Helpers.GetProperty(declaredType, "Length", nonPublic: false);
			if (length == null)
			{
				length = Helpers.GetProperty(declaredType, "Count", nonPublic: false);
			}
			if (length == null)
			{
				length = Helpers.GetProperty(ResolveIReadOnlyCollection(declaredType, array[0]), "Count", nonPublic: false);
			}
			if (length == null)
			{
				return false;
			}
		}
		add = Helpers.GetInstanceMethod(builderFactory.ReturnType, "Add", array);
		if (add == null)
		{
			return false;
		}
		finish = Helpers.GetInstanceMethod(builderFactory.ReturnType, "ToImmutable", Helpers.EmptyTypes);
		if (finish == null || finish.ReturnType == null || finish.ReturnType == type3)
		{
			return false;
		}
		if (!(finish.ReturnType == declaredType) && !Helpers.IsAssignableFrom(declaredType, finish.ReturnType))
		{
			return false;
		}
		addRange = Helpers.GetInstanceMethod(builderFactory.ReturnType, "AddRange", new Type[1] { declaredType });
		if (addRange == null)
		{
			Type type4 = model.MapType(typeof(IEnumerable<>), demand: false);
			if (type4 != null)
			{
				addRange = Helpers.GetInstanceMethod(builderFactory.ReturnType, "AddRange", new Type[1] { type4.MakeGenericType(array) });
			}
		}
		return true;
	}

	internal ImmutableCollectionDecorator(TypeModel model, Type declaredType, Type concreteType, IProtoSerializer tail, int fieldNumber, bool writePacked, WireType packedWireType, bool returnList, bool overwriteList, bool supportNull, MethodInfo builderFactory, PropertyInfo isEmpty, PropertyInfo length, MethodInfo add, MethodInfo addRange, MethodInfo finish)
		: base(model, declaredType, concreteType, tail, fieldNumber, writePacked, packedWireType, returnList, overwriteList, supportNull)
	{
		this.builderFactory = builderFactory;
		this.isEmpty = isEmpty;
		this.length = length;
		this.add = add;
		this.addRange = addRange;
		this.finish = finish;
	}

	public override object Read(object value, ProtoReader source)
	{
		object obj = builderFactory.Invoke(null, null);
		int field = source.FieldNumber;
		object[] array = new object[1];
		if (base.AppendToCollection && value != null && ((isEmpty != null) ? (!(bool)isEmpty.GetValue(value, null)) : ((byte)(int)length.GetValue(value, null) != 0)))
		{
			if (addRange != null)
			{
				array[0] = value;
				addRange.Invoke(obj, array);
			}
			else
			{
				foreach (object item in (ICollection)value)
				{
					array[0] = item;
					add.Invoke(obj, array);
				}
			}
		}
		if (packedWireType != WireType.None && source.WireType == WireType.String)
		{
			SubItemToken token = ProtoReader.StartSubItem(source);
			while (ProtoReader.HasSubValue(packedWireType, source))
			{
				array[0] = Tail.Read(null, source);
				add.Invoke(obj, array);
			}
			ProtoReader.EndSubItem(token, source);
		}
		else
		{
			do
			{
				array[0] = Tail.Read(null, source);
				add.Invoke(obj, array);
			}
			while (source.TryReadFieldHeader(field));
		}
		return finish.Invoke(obj, null);
	}
}
