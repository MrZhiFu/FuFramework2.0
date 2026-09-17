using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.WebSocket.Server;

/// <summary>
/// Represents a WebSocket session with methods for sending and closing connections.
/// </summary>
public class WebSocketSession : AppSession, IHandshakeRequiredSession
{
	/// <summary>
	/// Gets or sets a value indicating whether the handshake is completed.
	/// </summary>
	public bool Handshaked { get; internal set; }

	/// <summary>
	/// Gets the HTTP header associated with the WebSocket session.
	/// </summary>
	public HttpHeader HttpHeader { get; internal set; }

	/// <summary>
	/// Gets the path of the WebSocket session.
	/// </summary>
	public string Path => HttpHeader.Path;

	/// <summary>
	/// Gets or sets the sub-protocol used in the WebSocket session.
	/// </summary>
	public string SubProtocol { get; internal set; }

	internal ISubProtocolHandler SubProtocolHandler { get; set; }

	/// <summary>
	/// Gets the time when the close handshake started.
	/// </summary>
	public DateTime CloseHandshakeStartTime { get; private set; }

	internal CloseStatus CloseStatus { get; set; }

	internal IPackageEncoder<WebSocketPackage> MessageEncoder { get; set; }

	/// <summary>
	/// Occurs when the close handshake starts.
	/// </summary>
	public event EventHandler CloseHandshakeStarted;

	/// <summary>
	/// Sends a WebSocket package asynchronously.
	/// </summary>
	/// <param name="message">The WebSocket package to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public virtual ValueTask SendAsync(WebSocketPackage message, CancellationToken cancellationToken = default(CancellationToken))
	{
		return base.Connection.SendAsync(MessageEncoder, message, cancellationToken);
	}

	/// <summary>
	/// Sends a text message asynchronously.
	/// </summary>
	/// <param name="message">The text message to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public virtual ValueTask SendAsync(string message, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(new WebSocketPackage
		{
			OpCode = OpCode.Text,
			Message = message
		}, cancellationToken);
	}

	/// <summary>
	/// Sends binary data asynchronously.
	/// </summary>
	/// <param name="data">The binary data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public override ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(new ReadOnlySequence<byte>(data), cancellationToken);
	}

	/// <summary>
	/// Sends binary data asynchronously.
	/// </summary>
	/// <param name="data">The binary data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public override ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(new WebSocketPackage
		{
			OpCode = OpCode.Binary,
			Data = new ReadOnlySequence<byte>(data)
		}, cancellationToken);
	}

	/// <summary>
	/// Sends binary data asynchronously.
	/// </summary>
	/// <param name="data">The binary data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public virtual ValueTask SendAsync(ReadOnlySequence<byte> data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(new WebSocketPackage
		{
			OpCode = OpCode.Binary,
			Data = data
		}, cancellationToken);
	}

	/// <summary>
	/// Closes the WebSocket session asynchronously with the specified reason.
	/// </summary>
	/// <param name="reason">The reason for closing the session.</param>
	/// <param name="reasonText">The reason text.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	public ValueTask CloseAsync(CloseReason reason, string reasonText = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		short num = (short)reason;
		CloseStatus closeStatus = new CloseStatus
		{
			Reason = reason
		};
		int num2 = 0;
		if (!string.IsNullOrEmpty(reasonText))
		{
			num2 = Encoding.UTF8.GetMaxByteCount(reasonText.Length);
		}
		byte[] array = new byte[num2 + 2];
		array[0] = (byte)(num / 256);
		array[1] = (byte)(num % 256);
		int num3 = 2;
		if (!string.IsNullOrEmpty(reasonText))
		{
			closeStatus.ReasonText = reasonText;
			Span<byte> bytes = new Span<byte>(array, 2, array.Length - 2);
			num3 += Encoding.UTF8.GetBytes(reasonText.AsSpan(), bytes);
		}
		CloseStatus = closeStatus;
		CloseHandshakeStartTime = DateTime.Now;
		OnCloseHandshakeStarted();
		return SendAsync(new WebSocketPackage
		{
			OpCode = OpCode.Close,
			Data = new ReadOnlySequence<byte>(array, 0, num3)
		}, cancellationToken);
	}

	private void OnCloseHandshakeStarted()
	{
		this.CloseHandshakeStarted?.Invoke(this, EventArgs.Empty);
	}

	internal void CloseWithoutHandshake()
	{
		base.CloseAsync(FuFramework.SuperSocket.Connection.CloseReason.LocalClosing).DoNotAwait();
	}

	/// <summary>
	/// Closes the WebSocket session asynchronously.
	/// </summary>
	/// <param name="closeReason">The reason for closing the connection.</param>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	public override async ValueTask CloseAsync(FuFramework.SuperSocket.Connection.CloseReason closeReason)
	{
		CloseStatus closeStatus = CloseStatus;
		if (closeStatus != null)
		{
			bool remoteInitiated = closeStatus.RemoteInitiated;
			await base.CloseAsync(remoteInitiated ? FuFramework.SuperSocket.Connection.CloseReason.RemoteClosing : FuFramework.SuperSocket.Connection.CloseReason.LocalClosing);
			return;
		}
		try
		{
			await CloseAsync(CloseReason.NormalClosure);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Closes the WebSocket session asynchronously with a normal closure reason.
	/// </summary>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	public override async ValueTask CloseAsync()
	{
		await CloseAsync(CloseReason.NormalClosure);
	}
}
