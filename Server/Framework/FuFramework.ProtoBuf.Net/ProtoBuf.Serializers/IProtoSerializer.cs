using System;

namespace ProtoBuf.Serializers;

internal interface IProtoSerializer
{
	/// <summary>
	/// The type that this serializer is intended to work for.
	/// </summary>
	Type ExpectedType { get; }

	/// <summary>
	/// Indicates whether a Read operation <em>replaces</em> the existing value, or
	/// <em>extends</em> the value. If false, the "value" parameter to Read is
	/// discarded, and should be passed in as null.
	/// </summary>
	bool RequiresOldValue { get; }

	/// <summary>
	/// Now all Read operations return a value (although most do); if false no
	/// value should be expected.
	/// </summary>
	bool ReturnsValue { get; }

	/// <summary>
	/// Perform the steps necessary to serialize this data.
	/// </summary>
	/// <param name="value">The value to be serialized.</param>
	/// <param name="dest">The writer entity that is accumulating the output data.</param>
	void Write(object value, ProtoWriter dest);

	/// <summary>
	/// Perform the steps necessary to deserialize this data.
	/// </summary>
	/// <param name="value">The current value, if appropriate.</param>
	/// <param name="source">The reader providing the input data.</param>
	/// <returns>The updated / replacement value.</returns>
	object Read(object value, ProtoReader source);
}
