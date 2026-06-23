using System;
using System.ComponentModel;

namespace ProtoBuf;

/// <summary>Specifies a method on the root-contract in an hierarchy to be invoked after serialization.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[ImmutableObject(true)]
public sealed class ProtoAfterSerializationAttribute : Attribute
{
}
