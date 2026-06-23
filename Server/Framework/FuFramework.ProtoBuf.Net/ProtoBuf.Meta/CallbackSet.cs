using System;
using System.Reflection;

namespace ProtoBuf.Meta;

/// <summary>
/// Represents the set of serialization callbacks to be used when serializing/deserializing a type.
/// </summary>
public class CallbackSet
{
	private readonly MetaType metaType;

	private MethodInfo beforeSerialize;

	private MethodInfo afterSerialize;

	private MethodInfo beforeDeserialize;

	private MethodInfo afterDeserialize;

	internal MethodInfo this[TypeModel.CallbackType callbackType] => callbackType switch
	{
		TypeModel.CallbackType.BeforeSerialize => beforeSerialize, 
		TypeModel.CallbackType.AfterSerialize => afterSerialize, 
		TypeModel.CallbackType.BeforeDeserialize => beforeDeserialize, 
		TypeModel.CallbackType.AfterDeserialize => afterDeserialize, 
		_ => throw new ArgumentException("Callback type not supported: " + callbackType, "callbackType"), 
	};

	/// <summary>Called before serializing an instance</summary>
	public MethodInfo BeforeSerialize
	{
		get
		{
			return beforeSerialize;
		}
		set
		{
			beforeSerialize = SanityCheckCallback(metaType.Model, value);
		}
	}

	/// <summary>Called before deserializing an instance</summary>
	public MethodInfo BeforeDeserialize
	{
		get
		{
			return beforeDeserialize;
		}
		set
		{
			beforeDeserialize = SanityCheckCallback(metaType.Model, value);
		}
	}

	/// <summary>Called after serializing an instance</summary>
	public MethodInfo AfterSerialize
	{
		get
		{
			return afterSerialize;
		}
		set
		{
			afterSerialize = SanityCheckCallback(metaType.Model, value);
		}
	}

	/// <summary>Called after deserializing an instance</summary>
	public MethodInfo AfterDeserialize
	{
		get
		{
			return afterDeserialize;
		}
		set
		{
			afterDeserialize = SanityCheckCallback(metaType.Model, value);
		}
	}

	/// <summary>
	/// True if any callback is set, else False
	/// </summary>
	public bool NonTrivial
	{
		get
		{
			if (!(beforeSerialize != null) && !(beforeDeserialize != null) && !(afterSerialize != null))
			{
				return afterDeserialize != null;
			}
			return true;
		}
	}

	internal CallbackSet(MetaType metaType)
	{
		this.metaType = metaType ?? throw new ArgumentNullException("metaType");
	}

	internal static bool CheckCallbackParameters(TypeModel model, MethodInfo method)
	{
		ParameterInfo[] parameters = method.GetParameters();
		for (int i = 0; i < parameters.Length; i++)
		{
			Type parameterType = parameters[i].ParameterType;
			if (!(parameterType == model.MapType(typeof(SerializationContext))) && !(parameterType == model.MapType(typeof(Type))))
			{
				return false;
			}
		}
		return true;
	}

	private MethodInfo SanityCheckCallback(TypeModel model, MethodInfo callback)
	{
		metaType.ThrowIfFrozen();
		if (callback == null)
		{
			return callback;
		}
		if (callback.IsStatic)
		{
			throw new ArgumentException("Callbacks cannot be static", "callback");
		}
		if (callback.ReturnType != model.MapType(typeof(void)) || !CheckCallbackParameters(model, callback))
		{
			throw CreateInvalidCallbackSignature(callback);
		}
		return callback;
	}

	internal static Exception CreateInvalidCallbackSignature(MethodInfo method)
	{
		return new NotSupportedException("Invalid callback signature in " + method.DeclaringType.FullName + "." + method.Name);
	}
}
