using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.ClientEngine;

public interface IClientSession : IGameAppSession
{
	Socket Socket { get; }

	IProxyConnector Proxy { get; set; }

	int ReceiveBufferSize { get; set; }

	int SendingQueueSize { get; set; }

	event EventHandler Connected;

	event EventHandler Closed;

	event EventHandler<ErrorEventArgs> Error;

	event EventHandler<DataEventArgs> DataReceived;

	void Connect(EndPoint remoteEndPoint);

	void Send(ArraySegment<byte> segment);

	void Send(IList<ArraySegment<byte>> segments);

	void Send(byte[] data, int offset, int length);

	bool TrySend(ArraySegment<byte> segment);

	bool TrySend(IList<ArraySegment<byte>> segments);

	void Close();
}
