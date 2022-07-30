using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace CustomNetworking.Server;

public sealed class ClientSocket
{
	public readonly CancellationTokenSource ClientTokenSource = new();
	
	public delegate void OnDataReceivedEventHandler( MemoryStream stream );
	public event OnDataReceivedEventHandler? OnDataReceived;
	public delegate void OnMessageReceivedEventHandler( string message );
	public event OnMessageReceivedEventHandler? OnMessageReceived;
	
	private readonly WebSocket _socket;
	private readonly ConcurrentQueue<(WebSocketMessageType, byte[])> _dataQueue = new();
	private Task _sendTask = Task.CompletedTask;
	private Task _receiveTask = Task.CompletedTask;

	public ClientSocket( WebSocket socket )
	{
		_socket = socket;
	}

	public async Task CloseAsync( WebSocketCloseReason reason = WebSocketCloseReason.NormalClose )
	{
		ClientTokenSource.Cancel();
		await _socket.CloseAsync( reason );
	}

	public void Send( byte[] data )
	{
		_dataQueue.Enqueue( (WebSocketMessageType.Binary, data) );
	}

	public void Send( string message )
	{
		_dataQueue.Enqueue( (WebSocketMessageType.Text, Encoding.UTF8.GetBytes( message )) );
	}
	
	public async Task HandleConnectionAsync()
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
			Program.Logger.Enqueue( e.ToString() );
			await CloseAsync( WebSocketCloseReason.UnexpectedCondition );
		}
		finally
		{
			_socket.Dispose();
		}
	}

	private async Task HandleReadAsync()
	{
		var message = await _socket.ReadMessageAsync( ClientTokenSource.Token ).ConfigureAwait( false );
#if DEBUG
		NetworkServer.Instance.MessagesReceived++;
#endif
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
				OnDataReceived?.Invoke( stream );
				message.Close();
				stream.Close();
				break;
			case WebSocketMessageType.Text:
				var sReader = new StreamReader( message, new UTF8Encoding( false, false ) );
				var messageText = await sReader.ReadToEndAsync().ConfigureAwait( false );
				sReader.Close();
				OnMessageReceived?.Invoke( messageText );
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof(message) );
		}
	}

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
				throw new ArgumentOutOfRangeException( nameof(data.Item1) );
		}
	}
}
