using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetBolt.WebSocket.Enums;
using NetBolt.WebSocket.Extensions;
using NetBolt.WebSocket.Utility;

namespace NetBolt.WebSocket;

/// <summary>
/// A basic implementation of a web socket client wrapper around a <see cref="TcpClient"/>.
/// </summary>
public class WebSocketClient : IWebSocketClient
{
	/// <summary>
	/// Whether or not this client is connected to the server.
	/// <remarks>This does not mean they are capable of receiving messages yet. See <see cref="ConnectedAndUpgraded"/>.</remarks>
	/// </summary>
	public bool Connected { get; private set; }
	/// <summary>
	/// Whether or not this client is connected to the server and has been upgraded to the web socket protocol.
	/// </summary>
	public bool ConnectedAndUpgraded => Connected && _upgraded;
	
	/// <summary>
	/// The Internet Protocol (IP) address of the remote client.
	/// </summary>
	public IPAddress IpAddress
	{
		get => _socket.Client?.RemoteEndPoint is IPEndPoint ipEndPoint ? ipEndPoint.Address : IPAddress.None;
	}
	/// <summary>
	/// The port number of the remote clients socket.
	/// </summary>
	public int Port
	{
		get => _socket.Client?.RemoteEndPoint is IPEndPoint ipEndPoint ? ipEndPoint.Port : -1;
	}
	
	/// <summary>
	/// The server that this web socket is linked to.
	/// </summary>
	private readonly IWebSocketServer _server;
	/// <summary>
	/// The underlying socket this client controls.
	/// </summary>
	private readonly TcpClient _socket;
	/// <summary>
	/// The queue of messages to be sent to the client.
	/// </summary>
	private readonly ConcurrentQueue<(WebSocketOpCode, byte[])> _outgoingQueue = new();
	
	/// <summary>
	/// The asynchronous reading task this client is running.
	/// </summary>
	private Task<WebSocketError?> _readTask = Task.FromResult<WebSocketError?>( null );
	/// <summary>
	/// The asynchronous writing task this client is running.
	/// </summary>
	private Task _writeTask = Task.CompletedTask;
	/// <summary>
	/// Whether or not this client has been upgraded to the web socket protocol.
	/// </summary>
	private bool _upgraded;

	public WebSocketClient( TcpClient socket, IWebSocketServer server )
	{
		_socket = socket;
		_server = server;
	}
	
	/// <summary>
	/// Disconnects the client from the server.
	/// </summary>
	/// <param name="reason">The reason for the disconnect.</param>
	/// <param name="error">The error associated with the disconnect if applicable.</param>
	/// <returns>The async task that spawns from the invoke.</returns>
	public async Task DisconnectAsync( WebSocketDisconnectReason reason = WebSocketDisconnectReason.Requested, WebSocketError? error = null )
	{
		this.ThrowIfDisconnected();
		
		await _writeTask.ConfigureAwait( false );
		await Send( WebSocketOpCode.Close, WebSocketMessage.FormatCloseData( reason ) ).ConfigureAwait( false );
		Disconnect( reason, error );
	}
	
	/// <summary>
	/// Acts as the main loop for the client to handle its read/write logic.
	/// <remarks>The client should be disconnected once this is completed.</remarks>
	/// </summary>
	/// <returns>The async task that spawns from the invoke.</returns>
	public async Task HandleClientAsync()
	{
		Connected = true;
		OnConnected();
		WebSocketDisconnectReason? disconnectReason = null;
		WebSocketError? errorType = null;
		
		while ( Connected && disconnectReason is null )
		{
			if ( _readTask.IsCompleted )
				_readTask = HandleReadAsync();
			if ( _upgraded && !_outgoingQueue.IsEmpty )
				_writeTask = HandleWriteAsync();
			
			if ( !_readTask.IsCompleted && !_writeTask.IsCompleted )
				await Task.WhenAny( _readTask, _writeTask ).ConfigureAwait( false );

			if ( _readTask.Result is null )
				continue;

			disconnectReason = WebSocketDisconnectReason.Error;
			errorType = _readTask.Result.Value;
			break;
		}

		if ( Connected )
			await DisconnectAsync( disconnectReason ?? WebSocketDisconnectReason.None, errorType ).ConfigureAwait( false );
	}
	
	/// <summary>
	/// Sends a <see cref="WebSocketOpCode.Binary"/> message to the client.
	/// </summary>
	/// <param name="bytes">The binary data to send.</param>
	public void Send( byte[] bytes )
	{
		this.ThrowIfDisconnected();
		
		QueueSend( WebSocketOpCode.Binary, bytes );
	}

	/// <summary>
	/// Sends a <see cref="WebSocketOpCode.Text"/> message to the client.
	/// </summary>
	/// <param name="message">The message to send.</param>
	public void Send( string message )
	{
		this.ThrowIfDisconnected();
		
		QueueSend( WebSocketOpCode.Text, Encoding.UTF8.GetBytes( message ) );
	}

	public override string ToString()
	{
		var sb = new StringBuilder();
		sb.Append( nameof(WebSocketClient) );
		
		if ( _socket.Client is not null )
		{
			sb.Append( '(' );
			sb.Append( IpAddress );
			sb.Append( ':' );
			sb.Append( Port );
			sb.Append( ')' );
		}
		else
			sb.Append( "(Disconnected)" );

		return sb.ToString();
	}
	
	/// <summary>
	/// Invoked when the client has connected to the server.
	/// <remarks>This does not mean the client is capable of receiving messages yet. See <see cref="OnUpgraded"/>.</remarks>
	/// </summary>
	protected virtual void OnConnected()
	{
		_server.OnClientConnected( this );
		Console.WriteLine( "{0} has connected", this );
	}
	
	/// <summary>
	/// Invoked when the client has been upgraded to the web socket protocol.
	/// </summary>
	protected virtual void OnUpgraded()
	{
		_server.OnClientUpgraded( this );
		Console.WriteLine( "{0} has upgraded to WebSocket protocol", this  );
	}
	
	/// <summary>
	/// Invoked when the client has disconnected from the server.
	/// </summary>
	/// <param name="reason">The reason for disconnecting.</param>
	/// <param name="error">The error associated with the disconnect.</param>
	protected virtual void OnDisconnected( WebSocketDisconnectReason reason, WebSocketError? error )
	{
		_server.OnClientDisconnected( this, reason, error );
		Console.WriteLine( "{0} was disconnected for reason: {1}", this, reason );
		if ( reason == WebSocketDisconnectReason.Error )
			Console.WriteLine( "\tError was {0}", error );
	}
	
	/// <summary>
	/// Invoked when a <see cref="WebSocketOpCode.Binary"/> message has been received.
	/// </summary>
	/// <param name="bytes">The data that was sent by the client.</param>
	protected virtual void OnData( ReadOnlySpan<byte> bytes )
	{
		Console.WriteLine( "{0} sent {1} bytes", this, bytes.Length );
	}
	
	/// <summary>
	/// Invoked when a <see cref="WebSocketOpCode.Text"/> message has been received.
	/// </summary>
	/// <param name="message">The message that was sent by the client.</param>
	protected virtual void OnMessage( string message )
	{
		Console.WriteLine( "{0}: {1}" , this, message );
	}
	
	/// <summary>
	/// Handles reading a message from the client socket.
	/// </summary>
	/// <returns>The error that was experienced if applicable.</returns>
	private async Task<WebSocketError?> HandleReadAsync()
	{
		if ( !_socket.TryGetStream( out var stream ) )
			return WebSocketError.StreamDisposed;
		
		while ( !stream.DataAvailable && Connected )
			await Task.Delay( 1 ).ConfigureAwait( false );

		if ( !Connected )
			return null;

		var bytes = new byte[_socket.Available];
		_ = await stream.ReadAsync( bytes ).ConfigureAwait( false );
		if ( !Connected )
			return null;

		if ( !_upgraded )
		{
			var success = await HandleHandshake( bytes );
			if ( !success )
				return !Connected ? null : WebSocketError.UpgradeFail;

			_upgraded = true;
			OnUpgraded();
			return null;
		}
			
		var disconnectReason = ParseMessage( bytes );
		return !Connected ? null : disconnectReason;
	}
	
	/// <summary>
	/// Handles writing messages to the client socket.
	/// </summary>
	private async Task HandleWriteAsync()
	{
		while ( _outgoingQueue.TryDequeue( out var data ) )
		{
			await Send( data.Item1, data.Item2 ).ConfigureAwait( false );
			if ( data.Item1 != WebSocketOpCode.Close )
				continue;

			Disconnect( WebSocketDisconnectReason.Requested );
			return;
		}
	}
	
	/// <summary>
	/// Handles the initial handshake to the client.
	/// </summary>
	/// <param name="bytes">The handshake message received.</param>
	/// <returns>Whether or not the handshake succeeded.</returns>
	private async ValueTask<bool> HandleHandshake( byte[] bytes )
	{
		if ( !_socket.TryGetStream( out var stream ) )
			return false;
		
		var request = Encoding.UTF8.GetString( bytes );
		if ( !request.StartsWith( "GET" ) )
			return false;
				
		var response = WebSocketHandshake.GetUpgradeResponse( request );
		await stream.WriteAsync( response ).ConfigureAwait( false );
		return Connected;
	}
	
	/// <summary>
	/// Parses an incoming message and handles it.
	/// </summary>
	/// <param name="bytes">The message received.</param>
	/// <returns>The error that was experienced if applicable.</returns>
	private WebSocketError? ParseMessage( byte[] bytes )
	{
		var finished = (bytes[0] & 0b10000000) != 0;
		if ( !finished )
			return WebSocketError.MessageUnfinished;

		var mask = (bytes[1] & 0b10000000) != 0;
		if ( !mask )
			return WebSocketError.MissingMask;

		var opCode = (WebSocketOpCode)(bytes[0] & 0b00001111);
		var offset = 2UL;
		var messageLength = bytes[1] & 0b01111111UL;

		switch ( messageLength )
		{
			case 126:
				messageLength = BitConverter.ToUInt16( new[] {bytes[3], bytes[2]}, 0 );
				offset = 4;
				break;
			case 127:
				messageLength = BitConverter.ToUInt64( new[] {bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2]}, 0 );
				offset = 10;
				break;
		}

		if ( bytes.Length > 0 )
			bytes = WebSocketMessage.Decode( bytes, (int)messageLength, (int)offset );
		
		HandleMessageType( opCode, bytes );
		return null;
	}
	
	/// <summary>
	/// Handles an incoming message.
	/// </summary>
	/// <param name="opCode">The <see cref="WebSocketOpCode"/> associated with the message.</param>
	/// <param name="bytes">The data payload.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the <see cref="opCode"/> is invalid.</exception>
	private void HandleMessageType( WebSocketOpCode opCode, ReadOnlySpan<byte> bytes )
	{
		try
		{
			switch ( opCode )
			{
				case WebSocketOpCode.Continuation:
					break;
				case WebSocketOpCode.Text:
					var message = Encoding.UTF8.GetString( bytes );
					if ( message == "disconnect" )
					{
						_ = DisconnectAsync();
						break;
					}

					OnMessage( message );
					break;
				case WebSocketOpCode.Binary:
					OnData( bytes );
					break;
				case WebSocketOpCode.NonControl0:
					break;
				case WebSocketOpCode.NonControl1:
					break;
				case WebSocketOpCode.NonControl2:
					break;
				case WebSocketOpCode.NonControl3:
					break;
				case WebSocketOpCode.NonControl4:
					break;
				case WebSocketOpCode.Close:
					_ = DisconnectAsync();
					break;
				case WebSocketOpCode.Ping:
					break;
				case WebSocketOpCode.Pong:
					break;
				case WebSocketOpCode.Control0:
					break;
				case WebSocketOpCode.Control1:
					break;
				case WebSocketOpCode.Control2:
					break;
				case WebSocketOpCode.Control3:
					break;
				case WebSocketOpCode.Control4:
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof(opCode), opCode, null );
			}
		}
		catch ( Exception )
		{
			_ = DisconnectAsync( WebSocketDisconnectReason.Error, WebSocketError.HandlingException );
			throw;
		}
	}
	
	/// <summary>
	/// Disconnects the client and cleans up.
	/// </summary>
	/// <param name="reason">The reason for disconnecting.</param>
	/// <param name="error">The error associated with the disconnect.</param>
	private void Disconnect( WebSocketDisconnectReason reason, WebSocketError? error = null )
	{
		if ( !Connected )
			return;
		
		Connected = false;
		OnDisconnected( reason, error );
		_socket.Close();
		_outgoingQueue.Clear();
		_upgraded = false;
	}
	
	/// <summary>
	/// Queues a message for being sent to the client.
	/// </summary>
	/// <param name="opCode">The <see cref="WebSocketOpCode"/> of the message.</param>
	/// <param name="data">The data payload of the message.</param>
	private void QueueSend( WebSocketOpCode opCode, byte[] data )
	{
		_outgoingQueue.Enqueue( (opCode, data) );
	}
	
	/// <summary>
	/// sends a message to the client.
	/// </summary>
	/// <param name="opCode">The <see cref="WebSocketOpCode"/> of the message.</param>
	/// <param name="data">The data payload of the message.</param>
	private async Task Send( WebSocketOpCode opCode, byte[] data )
	{
		if ( !_socket.TryGetStream( out var stream ) )
			return;
		
		await stream.WriteAsync( WebSocketMessage.Frame( opCode, data ) ).ConfigureAwait( false );
	}
}
