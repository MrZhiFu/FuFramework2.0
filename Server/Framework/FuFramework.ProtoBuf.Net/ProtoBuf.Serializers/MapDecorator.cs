using System;
using System.Collections.Generic;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers;

internal class MapDecorator<TDictionary, TKey, TValue> : ProtoDecoratorBase where TDictionary : class, IDictionary<TKey, TValue>
{
	private readonly Type concreteType;

	private readonly IProtoSerializer keyTail;

	private readonly int fieldNumber;

	private readonly WireType wireType;

	private static readonly MethodInfo indexerSet = GetIndexerSetter();

	private static readonly TKey DefaultKey = ((typeof(TKey) == typeof(string)) ? ((TKey)(object)"") : default(TKey));

	private static readonly TValue DefaultValue = ((typeof(TValue) == typeof(string)) ? ((TValue)(object)"") : default(TValue));

	public override Type ExpectedType => typeof(TDictionary);

	public override bool ReturnsValue => true;

	public override bool RequiresOldValue => AppendToCollection;

	private bool AppendToCollection { get; }

	internal MapDecorator(TypeModel model, Type concreteType, IProtoSerializer keyTail, IProtoSerializer valueTail, int fieldNumber, WireType wireType, WireType keyWireType, WireType valueWireType, bool overwriteList)
		: base((DefaultValue == null) ? ((ProtoDecoratorBase)new TagDecorator(2, valueWireType, strict: false, valueTail)) : ((ProtoDecoratorBase)new DefaultValueDecorator(model, DefaultValue, new TagDecorator(2, valueWireType, strict: false, valueTail))))
	{
		this.wireType = wireType;
		this.keyTail = new DefaultValueDecorator(model, DefaultKey, new TagDecorator(1, keyWireType, strict: false, keyTail));
		this.fieldNumber = fieldNumber;
		this.concreteType = concreteType ?? typeof(TDictionary);
		if (keyTail.RequiresOldValue)
		{
			throw new InvalidOperationException("Key tail should not require the old value");
		}
		if (!keyTail.ReturnsValue)
		{
			throw new InvalidOperationException("Key tail should return a value");
		}
		if (!valueTail.ReturnsValue)
		{
			throw new InvalidOperationException("Value tail should return a value");
		}
		AppendToCollection = !overwriteList;
	}

	private static MethodInfo GetIndexerSetter()
	{
		PropertyInfo[] properties = typeof(TDictionary).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.Name != "Item" || propertyInfo.PropertyType != typeof(TValue))
			{
				continue;
			}
			ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
			if (indexParameters != null && indexParameters.Length == 1 && !(indexParameters[0].ParameterType != typeof(TKey)))
			{
				MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
				if (setMethod != null)
				{
					return setMethod;
				}
			}
		}
		throw new InvalidOperationException("Unable to resolve indexer for map");
	}

	public override object Read(object untyped, ProtoReader source)
	{
		TDictionary val = (AppendToCollection ? ((TDictionary)untyped) : null);
		if (val == null)
		{
			val = (TDictionary)Activator.CreateInstance(concreteType);
		}
		do
		{
			TKey key = DefaultKey;
			TValue val2 = DefaultValue;
			SubItemToken token = ProtoReader.StartSubItem(source);
			int num;
			while ((num = source.ReadFieldHeader()) > 0)
			{
				switch (num)
				{
				case 1:
					key = (TKey)keyTail.Read(null, source);
					break;
				case 2:
					val2 = (TValue)Tail.Read(Tail.RequiresOldValue ? ((object)val2) : null, source);
					break;
				default:
					source.SkipField();
					break;
				}
			}
			ProtoReader.EndSubItem(token, source);
			val[key] = val2;
		}
		while (source.TryReadFieldHeader(fieldNumber));
		return val;
	}

	public override void Write(object untyped, ProtoWriter dest)
	{
		foreach (KeyValuePair<TKey, TValue> item in (TDictionary)untyped)
		{
			ProtoWriter.WriteFieldHeader(fieldNumber, wireType, dest);
			SubItemToken token = ProtoWriter.StartSubItem(null, dest);
			if (item.Key != null)
			{
				keyTail.Write(item.Key, dest);
			}
			if (item.Value != null)
			{
				Tail.Write(item.Value, dest);
			}
			ProtoWriter.EndSubItem(token, dest);
		}
	}
}
