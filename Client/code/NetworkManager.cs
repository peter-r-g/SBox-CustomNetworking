using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;
using Sandbox;

namespace CustomNetworking.Client;

public class NetworkManager
{
	public static NetworkManager? Instance;
	
#if DEBUG
	public int MessagesReceived;
	public int MessagesSent;
#endif
	
	public readonly Dictionary<long, INetworkClient> Clients = new();
	
	public delegate void ConnectedEventHandler();
	public static event ConnectedEventHandler? Connected;

	public delegate void DisconnectedEventHandler();
	public static event DisconnectedEventHandler? Disconnected;
	
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
			throw new Exception( $"An instance of {nameof(NetworkManager)} already exists." );
		
		Instance = this;
		HandleMessage<RpcCallMessage>( HandleRpcCallMessage );
		HandleMessage<RpcCallResponseMessage>( HandleRpcCallResponseMessage );
		HandleMessage<PartialMessage>( HandlePartialMessage );
		HandleMessage<ShutdownMessage>( HandleShutdownMessage );
		HandleMessage<ClientListMessage>( HandleClientListMessage );
		HandleMessage<EntityListMessage>( HandleEntityListMessage );
		HandleMessage<ClientStateChangedMessage>( HandleClientStateChangedMessage );
		HandleMessage<EntityUpdateMessage>( HandleEntityUpdateMessage );
	}

	public async Task ConnectAsync()
	{
		if ( _webSocket is not null )
			Close();

		_webSocket = new WebSocket( SharedConstants.MaxBufferSize );
		_webSocket.OnDisconnected += WebSocketOnDisconnected;
		_webSocket.OnDataReceived += WebSocketOnDataReceived;
		_webSocket.OnMessageReceived += WebSocketOnMessageReceived;
		try
		{
			var headers = new Dictionary<string, string> {{"Steam", Local.PlayerId.ToString()}};
			await _webSocket.Connect( "ws://127.0.0.1:7087/", headers );
			Connected?.Invoke();
		}
		catch ( Exception e )
		{
			Log.Error( e );
			Close();
		}
	}

	public void Close()
	{
		if ( _webSocket is null )
			return;

		_webSocket.OnDisconnected -= WebSocketOnDisconnected;
		_webSocket.OnDataReceived -= WebSocketOnDataReceived;
		_webSocket.OnMessageReceived -= WebSocketOnMessageReceived;
		_webSocket.Dispose();
		Clients.Clear();
		_partialMessages.Clear();
#if DEBUG
		MessagesReceived = 0;
		MessagesSent = 0;
#endif
		
		Disconnected?.Invoke();
	}
	
	private void WebSocketOnDisconnected( int status, string reason )
	{
		Close();
	}
	
	private void WebSocketOnDataReceived( Span<byte> data )
	{
		var reader = new NetworkReader( new MemoryStream( data.ToArray() ) );
		var message = NetworkMessage.DeserializeMessage( reader );
		reader.Close();
		DispatchMessage( message );
	}

	private void WebSocketOnMessageReceived( string message )
	{
	}
	
	private void HandleRpcCallMessage( NetworkMessage message )
	{
		if ( message is not RpcCallMessage rpcCall )
			return;

		var type = TypeLibrary.GetTypeByName( rpcCall.ClassName );
		if ( type is null )
			throw new InvalidOperationException( $"Failed to handle RPC call (\"{rpcCall.ClassName}\" doesn't exist in the current assembly)." );

		// TODO: Support instance methods https://github.com/Facepunch/sbox-issues/issues/2079
		var method = TypeLibrary.FindStaticMethods( rpcCall.MethodName ).FirstOrDefault();
		if ( method is null )
			throw new InvalidOperationException( $"Failed to handle RPC call (\"{rpcCall.MethodName}\" does not exist on \"{type}\")." );
		
		if ( !method.Attributes.Any( attribute => attribute is Rpc.ClientAttribute ) )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to invoke a non-RPC method)." );
		
		var instance = MyGame.Current.EntityManager?.GetEntityById( rpcCall.EntityId );
		if ( instance is null && rpcCall.EntityId != -1 )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to call RPC on a non-existant entity)." );

		var parameters = new List<object>();
		parameters.AddRange( rpcCall.Parameters );
		if ( instance is not null )
			parameters.Insert( 0, instance );
		
		if ( rpcCall.CallGuid == Guid.Empty )
		{
			method.Invoke( null, parameters.ToArray() );
			return;
		}

		var returnValue = method.InvokeWithReturn<object?>( null, parameters.ToArray() );
		if ( returnValue is not INetworkable && returnValue is not null )
		{
			var failedMessage = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Failed );
			_ = SendToServer( failedMessage );
			throw new InvalidOperationException( $"Failed to handle RPC call (\"{rpcCall.MethodName}\" returned a non-networkable value)." );
		}
		
		var response = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Completed, returnValue as INetworkable ?? null );
		_ = SendToServer( response );
	}
	
	private void HandleRpcCallResponseMessage( NetworkMessage message )
	{
		if ( message is not RpcCallResponseMessage rpcResponse )
			return;

		Rpc.RpcResponses.Add( rpcResponse.CallGuid, rpcResponse );
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

		var bytes = ArrayPool<byte>.Shared.Rent( totalDataLength );
		var currentIndex = 0;
		foreach ( var part in messages )
		{
			Array.Copy( part.Data, 0, bytes, currentIndex, part.Data.Length );
			currentIndex += part.Data.Length;
		}
		
		_partialMessages.Remove( messageGuid );
		
		var reader = new NetworkReader( new MemoryStream( bytes ) );
		var finalMessage = NetworkMessage.DeserializeMessage( reader );
		reader.Close();
		ArrayPool<byte>.Shared.Return( bytes );
		
		DispatchMessage( finalMessage );
	}
		
	private void HandleShutdownMessage( NetworkMessage message )
	{
		if ( message is not ShutdownMessage )
			return;

		Close();
	}

	private void HandleClientListMessage( NetworkMessage message )
	{
		if ( message is not ClientListMessage clientListMessage )
			return;

		foreach ( var playerId in clientListMessage.ClientIds )
			Clients.Add( playerId, new NetworkClient( playerId ) );
	}
	
	private void HandleEntityListMessage( NetworkMessage message )
	{
		if ( message is not EntityListMessage entityListMessage )
			return;

		foreach ( var entityData in entityListMessage.EntityData )
		{
			var reader = new NetworkReader( new MemoryStream( entityData ) );
			MyGame.Current.EntityManager?.DeserializeAndAddEntity( reader );
			reader.Close();
		}
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
	
	private void HandleEntityUpdateMessage( NetworkMessage message )
	{
		if ( message is not EntityUpdateMessage entityUpdateMessage )
			return;

		var reader = new NetworkReader( new MemoryStream( entityUpdateMessage.EntityData ) );
		var entity = MyGame.Current.EntityManager?.GetEntityById( reader.ReadInt32() );
		if ( entity is null )
			throw new Exception( "Attempted to update an entity that does not exist." );
		
		reader.ReadNetworkableChanges( entity );
	}

	public async Task SendToServer( NetworkMessage message )
	{
		if ( _webSocket is null )
			return;

#if DEBUG
		MessagesSent++;
#endif
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkable( message );
		writer.Close();
		
		await _webSocket.Send( stream.ToArray() );
	}

	private void DispatchMessage( NetworkMessage message )
	{
#if DEBUG
		MessagesReceived++;
#endif
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
