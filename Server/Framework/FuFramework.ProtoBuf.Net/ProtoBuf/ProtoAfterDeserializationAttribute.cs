using System;
using System.ComponentModel;

namespace ProtoBuf;

/// <summary>Specifies a method on the root-contract in an hierarchy to be invoked after deserialization.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[ImmutableObject(true)]
public sealed class ProtoAfterDeserializationAttribute : Attribute
{
}
