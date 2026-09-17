using System.IO;

namespace FuFramework.SuperSocket.Connection;

internal interface IStreamConnection
{
	Stream Stream { get; }
}
