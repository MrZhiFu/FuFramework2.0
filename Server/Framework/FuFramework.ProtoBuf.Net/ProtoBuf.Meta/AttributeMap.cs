using System;
using System.Reflection;

namespace ProtoBuf.Meta;

internal abstract class AttributeMap
{
	private sealed class ReflectionAttributeMap : AttributeMap
	{
		private readonly Attribute attribute;

		public override object Target => attribute;

		public override Type AttributeType => attribute.GetType();

		public ReflectionAttributeMap(Attribute attribute)
		{
			this.attribute = attribute;
		}

		public override bool TryGet(string key, bool publicOnly, out object value)
		{
			MemberInfo[] instanceFieldsAndProperties = Helpers.GetInstanceFieldsAndProperties(attribute.GetType(), publicOnly);
			foreach (MemberInfo memberInfo in instanceFieldsAndProperties)
			{
				if (string.Equals(memberInfo.Name, key, StringComparison.OrdinalIgnoreCase))
				{
					if (memberInfo is PropertyInfo propertyInfo)
					{
						value = propertyInfo.GetValue(attribute, null);
						return true;
					}
					if (memberInfo is FieldInfo fieldInfo)
					{
						value = fieldInfo.GetValue(attribute);
						return true;
					}
					throw new NotSupportedException(memberInfo.GetType().Name);
				}
			}
			value = null;
			return false;
		}
	}

	public abstract Type AttributeType { get; }

	public abstract object Target { get; }

	public override string ToString()
	{
		return AttributeType?.FullName ?? "";
	}

	public abstract bool TryGet(string key, bool publicOnly, out object value);

	public bool TryGet(string key, out object value)
	{
		return TryGet(key, publicOnly: true, out value);
	}

	public static AttributeMap[] Create(TypeModel model, Type type, bool inherit)
	{
		object[] customAttributes = type.GetCustomAttributes(inherit);
		AttributeMap[] array = new AttributeMap[customAttributes.Length];
		for (int i = 0; i < customAttributes.Length; i++)
		{
			array[i] = new ReflectionAttributeMap((Attribute)customAttributes[i]);
		}
		return array;
	}

	public static AttributeMap[] Create(TypeModel model, MemberInfo member, bool inherit)
	{
		object[] customAttributes = member.GetCustomAttributes(inherit);
		AttributeMap[] array = new AttributeMap[customAttributes.Length];
		for (int i = 0; i < customAttributes.Length; i++)
		{
			array[i] = new ReflectionAttributeMap((Attribute)customAttributes[i]);
		}
		return array;
	}

	public static AttributeMap[] Create(TypeModel model, Assembly assembly)
	{
		object[] customAttributes = assembly.GetCustomAttributes(inherit: false);
		AttributeMap[] array = new AttributeMap[customAttributes.Length];
		for (int i = 0; i < customAttributes.Length; i++)
		{
			array[i] = new ReflectionAttributeMap((Attribute)customAttributes[i]);
		}
		return array;
	}
}
