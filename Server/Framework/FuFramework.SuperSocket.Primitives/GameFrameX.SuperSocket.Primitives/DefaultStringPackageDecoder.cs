using System.Buffers;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.Primitives;

/// <summary>
/// Decodes byte sequences into <see cref="T:FuFramework.SuperSocket.ProtoBase.StringPackageInfo" /> objects using a specified encoding.
/// </summary>
public class DefaultStringPackageDecoder : IPackageDecoder<StringPackageInfo>
{
	/// <summary>
	/// Gets the encoding used for decoding.
	/// </summary>
	public Encoding Encoding { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Primitives.DefaultStringPackageDecoder" /> class with UTF-8 encoding.
	/// </summary>
	public DefaultStringPackageDecoder()
		: this(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Primitives.DefaultStringPackageDecoder" /> class with the specified encoding.
	/// </summary>
	/// <param name="encoding">The encoding to use for decoding.</param>
	public DefaultStringPackageDecoder(Encoding encoding)
	{
		Encoding = encoding;
	}

	/// <summary>
	/// Decodes the specified byte sequence into a <see cref="T:FuFramework.SuperSocket.ProtoBase.StringPackageInfo" /> object.
	/// </summary>
	/// <param name="buffer">The byte sequence to decode.</param>
	/// <param name="context">The context for decoding (optional).</param>
	/// <returns>The decoded <see cref="T:FuFramework.SuperSocket.ProtoBase.StringPackageInfo" /> object.</returns>
	public StringPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context)
	{
		string[] array = buffer.GetString(Encoding).Split(' ', 2);
		string key = array[0];
		if (array.Length <= 1)
		{
			return new StringPackageInfo
			{
				Key = key
			};
		}
		return new StringPackageInfo
		{
			Key = key,
			Body = array[1],
			Parameters = array[1].Split(' ')
		};
	}
}
