using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Connection;

public class ConnectionOptions
{
	public int MaxPackageLength { get; set; } = 1048576;

	public int ReceiveBufferSize { get; set; } = 4096;

	public int SendBufferSize { get; set; } = 4096;

	public bool ReadAsDemand { get; set; }

	/// <summary>
	/// in milliseconds
	/// </summary>
	/// <value></value>
	public int ReceiveTimeout { get; set; }

	/// <summary>
	/// in milliseconds
	/// </summary>
	/// <value></value>
	public int SendTimeout { get; set; }

	public ILogger Logger { get; set; }

	public Pipe Input { get; set; }

	public Pipe Output { get; set; }

	public Dictionary<string, string> Values { get; set; }
}
