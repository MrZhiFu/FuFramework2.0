using System.Text;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server.Abstractions;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Provides a default string encoder for dependency injection, using server options for configuration.
/// </summary>
internal class DefaultStringEncoderForDI : DefaultStringEncoder
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.DefaultStringEncoderForDI" /> class with the specified server options.
	/// </summary>
	/// <param name="serverOptions">The server options containing the default text encoding.</param>
	public DefaultStringEncoderForDI(IOptions<ServerOptions> serverOptions)
		: base(serverOptions.Value?.DefaultTextEncoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
	{
	}
}
