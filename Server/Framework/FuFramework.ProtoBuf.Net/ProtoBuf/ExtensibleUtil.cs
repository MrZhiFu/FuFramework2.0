using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf;

/// <summary>
/// This class acts as an internal wrapper allowing us to do a dynamic
/// methodinfo invoke; an't put into Serializer as don't want on public
/// API; can't put into Serializer&lt;T&gt; since we need to invoke
/// across classes
/// </summary>
internal static class ExtensibleUtil
{
	/// <summary>
	/// All this does is call GetExtendedValuesTyped with the correct type for "instance";
	/// this ensures that we don't get issues with subclasses declaring conflicting types -
	/// the caller must respect the fields defined for the type they pass in.
	/// </summary>
	internal static IEnumerable<TValue> GetExtendedValues<TValue>(IExtensible instance, int tag, DataFormat format, bool singleton, bool allowDefinedTag)
	{
		foreach (TValue extendedValue in GetExtendedValues(RuntimeTypeModel.Default, typeof(TValue), instance, tag, format, singleton, allowDefinedTag))
		{
			yield return extendedValue;
		}
	}

	/// <summary>
	/// All this does is call GetExtendedValuesTyped with the correct type for "instance";
	/// this ensures that we don't get issues with subclasses declaring conflicting types -
	/// the caller must respect the fields defined for the type they pass in.
	/// </summary>
	internal static IEnumerable GetExtendedValues(TypeModel model, Type type, IExtensible instance, int tag, DataFormat format, bool singleton, bool allowDefinedTag)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (tag <= 0)
		{
			throw new ArgumentOutOfRangeException("tag");
		}
		IExtension extn = instance.GetExtensionObject(createIfMissing: false);
		if (extn == null)
		{
			yield break;
		}
		Stream stream = extn.BeginQuery();
		object value = null;
		ProtoReader reader = null;
		try
		{
			SerializationContext context = new SerializationContext();
			reader = ProtoReader.Create(stream, model, context, -1L);
			while (model.TryDeserializeAuxiliaryType(reader, format, tag, type, ref value, skipOtherFields: true, asListItem: true, autoCreate: false, insideList: false, null) && value != null)
			{
				if (!singleton)
				{
					yield return value;
					value = null;
				}
			}
			if (singleton && value != null)
			{
				yield return value;
			}
		}
		finally
		{
			ProtoReader.Recycle(reader);
			extn.EndQuery(stream);
		}
	}

	internal static void AppendExtendValue(TypeModel model, IExtensible instance, int tag, DataFormat format, object value)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		IExtension extensionObject = instance.GetExtensionObject(createIfMissing: true);
		if (extensionObject == null)
		{
			throw new InvalidOperationException("No extension object available; appended data would be lost.");
		}
		bool commit = false;
		Stream stream = extensionObject.BeginAppend();
		try
		{
			using (ProtoWriter protoWriter = ProtoWriter.Create(stream, model))
			{
				model.TrySerializeAuxiliaryType(protoWriter, null, format, tag, value, isInsideList: false, null);
				protoWriter.Close();
			}
			commit = true;
		}
		finally
		{
			extensionObject.EndAppend(stream, commit);
		}
	}
}
