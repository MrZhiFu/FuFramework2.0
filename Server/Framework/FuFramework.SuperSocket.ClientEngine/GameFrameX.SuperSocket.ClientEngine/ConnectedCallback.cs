using System;
using System.Net.Sockets;

namespace FuFramework.SuperSocket.ClientEngine;

public delegate void ConnectedCallback(Socket socket, object state, SocketAsyncEventArgs e, Exception exception);
