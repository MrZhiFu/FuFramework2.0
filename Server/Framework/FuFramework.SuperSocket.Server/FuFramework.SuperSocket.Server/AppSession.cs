using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Represents an application session that manages connection, state, and events.
/// </summary>
public class AppSession : IAppSession, IGameAppSession, ILogger, ILoggerAccessor
{
	private IConnection _connection;

	private Dictionary<object, object> _items;

	/// <summary>
	/// Gets the connection associated with the session.
	/// </summary>
	protected internal IConnection Connection => _connection;

	/// <summary>
	/// Gets the session ID.
	/// </summary>
	public string SessionID { get; private set; }

	/// <summary>
	/// Gets the start time of the session.
	/// </summary>
	public DateTimeOffset StartTime { get; private set; }

	/// <summary>
	/// Gets the current state of the session.
	/// </summary>
	public SessionState State { get; private set; }

	/// <summary>
	/// Gets the server information associated with the session.
	/// </summary>
	public bool IsConnected
	{
		get
		{
			IConnection connection = _connection;
			if (connection != null && connection.CloseReason.HasValue)
			{
				return false;
			}
			return State == SessionState.Connected;
		}
	}

	public IServerInfo Server { get; private set; }

	IConnection IAppSession.Connection => _connection;

	/// <summary>
	/// Gets or sets the data context for the session.
	/// </summary>
	public object DataContext { get; set; }

	/// <summary>
	/// Gets the remote endpoint of the session.
	/// </summary>
	public EndPoint RemoteEndPoint
	{
		get
		{
			IConnection connection = _connection;
			if (connection == null)
			{
				return null;
			}
			return connection.ProxyInfo?.SourceEndPoint ?? connection.RemoteEndPoint;
		}
	}

	/// <summary>
	/// Gets the local endpoint of the session.
	/// </summary>
	public EndPoint LocalEndPoint => _connection?.LocalEndPoint;

	/// <summary>
	/// Gets the last active time of the session.
	/// </summary>
	public DateTimeOffset LastActiveTime => _connection?.LastActiveTime ?? DateTimeOffset.MinValue;

	/// <summary>
	/// Gets or sets session-specific data by key.
	/// </summary>
	/// <param name="name">The key of the data.</param>
	/// <returns>The value associated with the key.</returns>
	public object this[object name]
	{
		get
		{
			return _items?.GetValueOrDefault(name);
		}
		set
		{
			lock (this)
			{
				Dictionary<object, object> dictionary = _items;
				if (dictionary == null)
				{
					dictionary = (_items = new Dictionary<object, object>());
				}
				dictionary[name] = value;
			}
		}
	}

	/// <summary>
	/// Gets the logger associated with the session.
	/// </summary>
	public ILogger Logger => this;

	/// <summary>
	/// Occurs when the session is connected.
	/// </summary>
	public event AsyncEventHandler Connected;

	/// <summary>
	/// Occurs when the session is closed.
	/// </summary>
	public event AsyncEventHandler<CloseEventArgs> Closed;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.AppSession" /> class.
	/// </summary>
	public AppSession()
	{
	}

	/// <summary>
	/// Initializes the session with the specified server and connection.
	/// </summary>
	/// <param name="server">The server information.</param>
	/// <param name="connection">The connection associated with the session.</param>
	void IAppSession.Initialize(IServerInfo server, IConnection connection)
	{
		if (connection is IConnectionWithSessionIdentifier connectionWithSessionIdentifier)
		{
			SessionID = connectionWithSessionIdentifier.SessionIdentifier;
		}
		else
		{
			SessionID = Guid.NewGuid().ToString();
		}
		Server = server;
		StartTime = DateTimeOffset.Now;
		_connection = connection;
		State = SessionState.Initialized;
	}

	/// <summary>
	/// Sends binary data asynchronously.
	/// </summary>
	/// <param name="data">The binary data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>ly.
	public virtual ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _connection.SendAsync(data, cancellationToken);
	}

	/// <summary>
	/// Sends binary data asynchronously.
	/// </summary>
	/// <param name="data">The binary data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public virtual ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _connection.SendAsync(data, cancellationToken);
	}

	/// <summary>
	/// Closes the session.
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	protected virtual ValueTask OnSessionClosedAsync(CloseEventArgs e)
	{
		return default(ValueTask);
	}

	internal async ValueTask FireSessionClosedAsync(CloseEventArgs e)
	{
		State = SessionState.Closed;
		await OnSessionClosedAsync(e);
		AsyncEventHandler<CloseEventArgs> asyncEventHandler = this.Closed;
		if (asyncEventHandler != null)
		{
			await asyncEventHandler(this, e);
		}
	}

	protected virtual ValueTask OnSessionConnectedAsync()
	{
		return default(ValueTask);
	}

	internal async ValueTask FireSessionConnectedAsync()
	{
		State = SessionState.Connected;
		await OnSessionConnectedAsync();
		AsyncEventHandler asyncEventHandler = this.Connected;
		if (asyncEventHandler != null)
		{
			await asyncEventHandler(this, EventArgs.Empty);
		}
	}

	ValueTask IAppSession.SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken)
	{
		return _connection.SendAsync(packageEncoder, package, cancellationToken);
	}

	void IAppSession.Reset()
	{
		ClearEvent(ref this.Connected);
		ClearEvent(ref this.Closed);
		_items?.Clear();
		State = SessionState.None;
		_connection = null;
		DataContext = null;
		StartTime = default(DateTimeOffset);
		Server = null;
		Reset();
	}

	protected virtual void Reset()
	{
	}

	private void ClearEvent<TEventHandler>(ref TEventHandler sessionEvent) where TEventHandler : Delegate
	{
		if (!(sessionEvent == null))
		{
			Delegate[] invocationList = sessionEvent.GetInvocationList();
			foreach (Delegate value in invocationList)
			{
				sessionEvent = Delegate.Remove(sessionEvent, value) as TEventHandler;
			}
		}
	}

	/// <summary>
	/// Closes the session asynchronously.
	/// </summary>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	public virtual async ValueTask CloseAsync()
	{
		await CloseAsync(CloseReason.LocalClosing);
	}

	/// <summary>
	/// Closes the session asynchronously with the specified reason.
	/// </summary>
	/// <param name="reason">The reason for closing the session.</param>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	public virtual async ValueTask CloseAsync(CloseReason reason)
	{
		IConnection connection = Connection;
		State = SessionState.Closed;
		if (connection == null)
		{
			return;
		}
		try
		{
			await connection.CloseAsync(reason);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Gets the logger associated with the session.
	/// </summary>
	private ILogger GetLogger()
	{
		return (Server as ILoggerAccessor).Logger;
	}

	void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		GetLogger().Log(logLevel, eventId, state, exception, (TState s, Exception? e) => "Session[" + SessionID + "]: " + formatter(s, e));
	}

	bool ILogger.IsEnabled(LogLevel logLevel)
	{
		return GetLogger().IsEnabled(logLevel);
	}

	IDisposable ILogger.BeginScope<TState>(TState state)
	{
		return GetLogger().BeginScope(state);
	}
}
