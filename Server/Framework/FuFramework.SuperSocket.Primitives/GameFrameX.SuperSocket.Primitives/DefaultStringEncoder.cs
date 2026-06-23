using System.Buffers;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.Primitives;

/// <summary>
/// Encodes strings into byte sequences using a specified encoding.
/// </summary>
public class DefaultStringEncoder : IPackageEncoder<string>
{
	private Encoding _encoding;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Primitives.DefaultStringEncoder" /> class with UTF-8 encoding.
	/// </summary>
	public DefaultStringEncoder()
		: this(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Primitives.DefaultStringEncoder" /> class with the specified encoding.
	/// </summary>
	/// <param name="encoding">The encoding to use for string encoding.</param>
	public DefaultStringEncoder(Encoding encoding)
	{
		_encoding = encoding;
	}

	/// <summary>
	/// Encodes the specified string into a byte sequence and writes it to the buffer writer.
	/// </summary>
	/// <param name="writer">The buffer writer to write the encoded bytes to.</param>
	/// <param name="pack">The string to encode.</param>
	/// <returns>The number of bytes written to the buffer writer.</returns>
	public int Encode(IBufferWriter<byte> writer, string pack)
	{
		return writer.Write(pack, _encoding);
	}
}
