using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetBolt.Shared.Utility;
using vtortola.WebSockets;

namespace NetBolt.Server;

/// <summary>
/// A clients socket connection.
/// </summary>
internal sealed class ClientSocket
{
	/// <summary>
	/// This clients cancellation source.
	/// </summary>
	internal readonly CancellationTokenSource ClientTokenSource = new();
	
	/// <summary>
	/// The event handler for <see cref="ClientSocket"/>.<see cref="ClientSocket.DataReceived"/>.
	/// </summary>
	public delegate void DataReceivedEventHandler( MemoryStream stream );
	/// <summary>
	/// Called when data has been received from this <see cref="ClientSocket"/>.
	/// </summary>
	public event DataReceivedEventHandler? DataReceived;
	/// <summary>
	/// The event handler for <see cref="ClientSocket"/>.<see cref="ClientSocket.MessageReceived"/>.
	/// </summary>
	public delegate void MessageReceivedEventHandler( string message );
	/// <summary>
	/// Called when a message has been received from this <see cref="ClientSocket"/>.
	/// </summary>
	public event MessageReceivedEventHandler? MessageReceived;
	
	/// <summary>
	/// The underlying socket used by the client.
	/// </summary>
	private readonly vtortola.WebSockets.WebSocket _socket;
	/// <summary>
	/// The data queue to send to the client.
	/// </summary>
	private readonly ConcurrentQueue<(WebSocketMessageType, byte[])> _dataQueue = new();
	/// <summary>
	/// A task for sending data to the client.
	/// </summary>
	private Task _sendTask = Task.CompletedTask;
	/// <summary>
	/// A task for receiving data from the client.
	/// </summary>
	private Task _receiveTask = Task.CompletedTask;

	public ClientSocket( vtortola.WebSockets.WebSocket socket )
	{
		_socket = socket;
	}

	/// <summary>
	/// Closes this web socket connection with the provided reason.
	/// </summary>
	/// <param name="reason">The reason for the connection closing.</param>
	public async Task CloseAsync( WebSocketCloseReason reason = WebSocketCloseReason.NormalClose )
	{
		ClientTokenSource.Cancel();
		await _socket.CloseAsync( reason );
	}

	/// <summary>
	/// Sends an array of bytes to the web socket client.
	/// </summary>
	/// <param name="data">The data to send to the client.</param>
	public void Send( byte[] data )
	{
		_dataQueue.Enqueue( (WebSocketMessageType.Binary, data) );
	}

	/// <summary>
	/// Sends a string message to the web socket client.
	/// </summary>
	/// <param name="message">The message to send to the client.</param>
	public void Send( string message )
	{
		_dataQueue.Enqueue( (WebSocketMessageType.Text, Encoding.UTF8.GetBytes( message )) );
	}
	
	/// <summary>
	/// Handles the client socket connected asynchronously.
	/// </summary>
	internal async Task HandleConnectionAsync()
	{
		try
		{
			NetworkServer.Instance.AcceptClient( long.Parse( _socket.HttpRequest.Headers.Get( "Steam" ) ), this );
			
			while ( _socket.IsConnected && !ClientTokenSource.Token.IsCancellationRequested )
			{
				if ( _sendTask.IsCompleted && !_dataQueue.IsEmpty )
					_sendTask = Task.Run( HandleWriteAsync, ClientTokenSource.Token );

				if ( _receiveTask.IsCompleted )
					_receiveTask = Task.Run( HandleReadAsync, ClientTokenSource.Token );
			}

			await CloseAsync();
		}
		catch ( Exception e )
		{
			Logging.Error( e );
			await CloseAsync( WebSocketCloseReason.UnexpectedCondition );
		}
		finally
		{
			_socket.Dispose();
		}
	}

	/// <summary>
	/// Handles reading data incoming from the client.
	/// </summary>
	private async Task HandleReadAsync()
	{
		var message = await _socket.ReadMessageAsync( ClientTokenSource.Token ).ConfigureAwait( false );
		NetworkServer.Instance.MessagesReceived++;
		if ( message is null )
		{
			await NetworkServer.Instance.AbandonClient( this );
			return;
		}
		
		switch ( message.MessageType )
		{
			case WebSocketMessageType.Binary:
				var stream = new MemoryStream();
				await message.CopyToAsync( stream );
				stream.Position = 0;
				DataReceived?.Invoke( stream );
				message.Close();
				stream.Close();
				break;
			case WebSocketMessageType.Text:
				var sReader = new StreamReader( message, new UTF8Encoding( false, false ) );
				var messageText = await sReader.ReadToEndAsync().ConfigureAwait( false );
				sReader.Close();
				MessageReceived?.Invoke( messageText );
				break;
			default:
				Logging.Error( "Got unexpected type of message while reading.", new ArgumentOutOfRangeException( nameof(message) ) );
				break;
		}
	}

	/// <summary>
	/// Handles writing data to the client.
	/// </summary>
	private async Task HandleWriteAsync()
	{
		if ( !_dataQueue.TryDequeue( out var data ) )
			return;
		
		switch ( data.Item1 )
		{
			case WebSocketMessageType.Binary:
				await _socket.WriteBytesAsync( data.Item2, ClientTokenSource.Token );
				break;
			case WebSocketMessageType.Text:
				await _socket.WriteStringAsync( Encoding.UTF8.GetString( data.Item2 ), ClientTokenSource.Token );
				break;
			default:
				Logging.Error( "Got unexpected type of message while writing.", new ArgumentOutOfRangeException( nameof(data.Item1) ) );
				break;
		}
	}
}
