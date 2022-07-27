using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;
using Sandbox;

namespace CustomNetworking.Client;

public class NetworkManager
{
	public static NetworkManager? Instance;
	
	public readonly Dictionary<long, INetworkClient> Clients = new();
	
	public delegate void ClientConnectedEventHandler( INetworkClient client );
	public static event ClientConnectedEventHandler? ClientConnected;
	
	public delegate void ClientDisconnectedEventHandler( INetworkClient client );
	public static event ClientDisconnectedEventHandler? ClientDisconnected;
	
	private WebSocket? _webSocket;
	private readonly Dictionary<Type, Action<NetworkMessage>> _messageHandlers = new();
	private readonly Dictionary<Guid, List<PartialMessage>> _partialMessages = new();

	public NetworkManager()
	{
		if ( Instance is not null )
			throw new Exception( $"An instance of {nameof(NetworkManager)} already exists" );
		
		Instance = this;
		HandleMessage<PartialMessage>( HandlePartialMessage );
		HandleMessage<ClientListMessage>( HandleClientListMessage );
		HandleMessage<ClientStateChangedMessage>( HandleClientStateChangedMessage );
	}

	public async Task ConnectAsync()
	{
		if ( _webSocket is not null )
			Close();

		_webSocket = new WebSocket( SharedConstants.MaxBufferSize );
		_webSocket.OnDataReceived += WebSocketOnDataReceived;
		_webSocket.OnMessageReceived += WebSocketOnMessageReceived;
		await _webSocket.Connect( "ws://127.0.0.1:7087/" );
	}

	public void Close()
	{
		if ( _webSocket is null )
			return;
		
		_webSocket.OnDataReceived -= WebSocketOnDataReceived;
		_webSocket.OnMessageReceived -= WebSocketOnMessageReceived;
		_webSocket.Dispose();
		Clients.Clear();
		_partialMessages.Clear();
	}
	
	private void WebSocketOnDataReceived( Span<byte> data )
	{
		var reader = new BinaryReader( new MemoryStream( data.ToArray() ) );
		var message = NetworkMessage.Deserialize( reader );
		reader.Close();
		DispatchMessage( message );
	}

	private void WebSocketOnMessageReceived( string message )
	{
		if ( message == "Verify" )
			_ = _webSocket!.Send( $"Steam: {Local.PlayerId}" );
	}

	private void HandlePartialMessage( NetworkMessage message )
	{
		if ( message is not PartialMessage partialMessage )
			return;

		var messageGuid = partialMessage.MessageGuid;
		if ( !_partialMessages.ContainsKey( messageGuid ) )
			_partialMessages.Add( messageGuid, new List<PartialMessage>() );

		var messages = _partialMessages[messageGuid];
		messages.Insert( partialMessage.Piece, partialMessage );
		if ( messages.Count < partialMessage.NumPieces )
			return;

		var totalDataLength = 0;
		foreach ( var part in messages )
			totalDataLength += part.Data.Length;

		var bytes = new byte[totalDataLength];
		var currentIndex = 0;
		foreach ( var part in messages )
		{
			Array.Copy( part.Data, 0, bytes, currentIndex, part.Data.Length );
			currentIndex += part.Data.Length;
		}
		
		_partialMessages.Remove( messageGuid );
		
		var reader = new BinaryReader( new MemoryStream( bytes ) );
		var finalMessage = NetworkMessage.Deserialize( reader );
		reader.Close();
		DispatchMessage( finalMessage );
	}

	private void HandleClientListMessage( NetworkMessage message )
	{
		if ( message is not ClientListMessage clientListMessage )
			return;

		foreach ( var playerId in clientListMessage.ClientIds )
			Clients.Add( playerId, new NetworkClient( playerId ) );
	}
	
	private void HandleClientStateChangedMessage( NetworkMessage message )
	{
		if ( message is not ClientStateChangedMessage clientStateChangedMessage )
			return;

		switch ( clientStateChangedMessage.ClientState )
		{
			case ClientState.Connected:
				var client = new NetworkClient( clientStateChangedMessage.ClientId );
				Clients.Add( clientStateChangedMessage.ClientId, client );
				ClientConnected?.Invoke( client );
				break;
			case ClientState.Disconnected:
				if ( !Clients.TryGetValue( clientStateChangedMessage.ClientId, out var disconnectedClient ) )
					return;
				
				Clients.Remove( clientStateChangedMessage.ClientId );
				ClientDisconnected?.Invoke( disconnectedClient );
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof(clientStateChangedMessage.ClientState) );
		}
	}

	public async Task SendToServer( NetworkMessage message )
	{
		if ( _webSocket is null )
			return;
		
		var stream = new MemoryStream();
		var writer = new BinaryWriter( stream );
		message.Serialize( writer );
		writer.Close();
		
		await _webSocket.Send( stream.ToArray() );
	}

	private void DispatchMessage( NetworkMessage message )
	{
		if ( !_messageHandlers.TryGetValue( message.GetType(), out var cb ) )
			throw new Exception( $"Unhandled message {message.GetType()}." );
		
		cb.Invoke( message );
	}

	public void HandleMessage<T>( Action<NetworkMessage> cb ) where T : NetworkMessage
	{
		var messageType = typeof(T);
		if ( _messageHandlers.ContainsKey( messageType ) )
			throw new Exception( $"Message type {messageType} is already being handled." );

		_messageHandlers.Add( messageType, cb );
	}

	public INetworkClient? GetClientById( long playerId )
	{
		return playerId == -1 ? null : Clients[playerId];
	}
}
