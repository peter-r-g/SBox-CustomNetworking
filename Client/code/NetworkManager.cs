using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.RemoteProcedureCalls;
using CustomNetworking.Shared.Utility;
using Sandbox;
using Logging = CustomNetworking.Shared.Utility.Logging;

namespace CustomNetworking.Client;

public class NetworkManager
{
	public static NetworkManager? Instance;
	
#if DEBUG
	public int MessagesReceived;
	public int MessagesSent;

	public readonly Dictionary<Type, int> MessageTypesReceived = new();
#endif
	
	public readonly Dictionary<long, INetworkClient> Clients = new();
	public INetworkClient LocalClient => GetClientById( _localClientId )!;
	public readonly EntityManager SharedEntityManager = new();

	public bool Connected { get; private set; }

	public delegate void ConnectedEventHandler();
	public static event ConnectedEventHandler? ConnectedToServer;

	public delegate void DisconnectedEventHandler();
	public static event DisconnectedEventHandler? DisconnectedFromServer;
	
	public delegate void ClientConnectedEventHandler( INetworkClient client );
	public static event ClientConnectedEventHandler? ClientConnected;
	
	public delegate void ClientDisconnectedEventHandler( INetworkClient client );
	public static event ClientDisconnectedEventHandler? ClientDisconnected;

	private WebSocket _webSocket;
	private readonly Dictionary<Type, Action<NetworkMessage>> _messageHandlers = new();
	private readonly Dictionary<Guid, List<PartialMessage>> _partialMessages = new();
	private readonly Queue<byte[]> _incomingQueue = new();
	private readonly Queue<NetworkMessage> _outgoingQueue = new();
	private readonly Stopwatch _pawnSw = Stopwatch.StartNew();
	private bool _localPawnChanged;
	private long _localClientId;

	public NetworkManager()
	{
		if ( Instance is not null )
			Logging.Fatal( new InvalidOperationException( $"An instance of {nameof(NetworkManager)} already exists." ) );
		
		Instance = this;
		_webSocket = new WebSocket( SharedConstants.MaxBufferSize );
		_webSocket.OnDisconnected += WebSocketOnDisconnected;
		_webSocket.OnDataReceived += WebSocketOnDataReceived;
		_webSocket.OnMessageReceived += WebSocketOnMessageReceived;
		
		HandleMessage<RpcCallMessage>( Rpc.HandleRpcCallMessage );
		HandleMessage<RpcCallResponseMessage>( Rpc.HandleRpcCallResponseMessage );
		HandleMessage<PartialMessage>( HandlePartialMessage );
		HandleMessage<MultiMessage>( HandleMultiMessage );
		HandleMessage<ShutdownMessage>( HandleShutdownMessage );
		HandleMessage<ClientListMessage>( HandleClientListMessage );
		HandleMessage<EntityListMessage>( HandleEntityListMessage );
		HandleMessage<CreateEntityMessage>( HandleCreateEntityMessage );
		HandleMessage<DeleteEntityMessage>( HandleDeleteEntityMessage );
		HandleMessage<ClientStateChangedMessage>( HandleClientStateChangedMessage );
		HandleMessage<ClientPawnChangedMessage>( HandleClientPawnChangedMessage );
		HandleMessage<MultiEntityUpdateMessage>( HandleMultiEntityUpdateMessage );
	}

	public async Task ConnectAsync( string uri, int port, bool secure )
	{
		if ( Connected )
			Close();
		
		try
		{
			_localClientId = Random.Shared.NextInt64();
			var headers = new Dictionary<string, string> {{"Steam", _localClientId.ToString()}};
			var webSocketUri = (secure ? "wss://" : "ws://") + uri + ':' + port + '/' ;
			await _webSocket.Connect( webSocketUri, headers );
			Connected = true;
			ConnectedToServer?.Invoke();
		}
		catch ( Exception e )
		{
			Logging.Error( e );
			Close();
		}
	}

	public void Close()
	{
		Connected = false;
		_webSocket.Dispose();
		_webSocket = new WebSocket( SharedConstants.MaxBufferSize );
		Clients.Clear();
		SharedEntityManager.DeleteAllEntities();
		_partialMessages.Clear();
		
#if DEBUG
		MessagesReceived = 0;
		MessagesSent = 0;
		MessageTypesReceived.Clear();
#endif
		
		DisconnectedFromServer?.Invoke();
	}

	public void Update()
	{
		foreach ( var entity in SharedEntityManager.Entities )
			entity.Update();

		if ( !_localPawnChanged || _pawnSw.Elapsed.TotalMilliseconds < 100 )
			return;

		if ( LocalClient.Pawn is null )
		{
			Logging.Error( "Pawn was marked as changed but the client has no pawn.", new InvalidOperationException() );
			_localPawnChanged = false;
			return;
		}
			
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkableChanges( LocalClient.Pawn );
		writer.Close();

		SendToServer( new ClientPawnUpdateMessage( stream.ToArray() ) );
		_localPawnChanged = false;
		_pawnSw.Restart();
	}
	
	private void WebSocketOnDisconnected( int status, string reason )
	{
		Close();
	}
	
	private void WebSocketOnDataReceived( Span<byte> data )
	{
#if DEBUG
		MessagesReceived++;
#endif
		
		_incomingQueue.Enqueue( data.ToArray() );
	}

	private void WebSocketOnMessageReceived( string message )
	{
	}

	internal void DispatchIncoming()
	{
		while ( _incomingQueue.TryDequeue( out var bytes ) )
		{
			var reader = new NetworkReader( new MemoryStream( bytes ) );
			var message = NetworkMessage.DeserializeMessage( reader );
			reader.Close();
			DispatchMessage( message );
		}
	}

	internal void DispatchOutgoing()
	{
		while ( _outgoingQueue.TryDequeue( out var message ) )
		{
#if DEBUG
			MessagesSent++;
#endif
			var stream = new MemoryStream();
			var writer = new NetworkWriter( stream );
			writer.WriteNetworkable( message );
			writer.Close();
		
			_ = _webSocket?.Send( stream.ToArray() );	
		}
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

	private void HandleMultiMessage( NetworkMessage message )
	{
		if ( message is not MultiMessage multiMessage )
			return;
		
		foreach ( var msg in multiMessage.Messages )
			DispatchMessage( msg );
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

		foreach ( var (clientId, pawnId) in clientListMessage.ClientIds )
		{
			var client = new NetworkClient( clientId ) {Pawn = SharedEntityManager.GetEntityById( pawnId )};
			Clients.Add( clientId, client );
		}
	}
	
	private void HandleEntityListMessage( NetworkMessage message )
	{
		if ( message is not EntityListMessage entityListMessage )
			return;

		foreach ( var entityData in entityListMessage.EntityData )
		{
			var reader = new NetworkReader( new MemoryStream( entityData ) );
			SharedEntityManager.DeserializeAndAddEntity( reader );
			reader.Close();
		}
	}

	private void HandleCreateEntityMessage( NetworkMessage message )
	{
		if ( message is not CreateEntityMessage createEntityMessage )
			return;

		SharedEntityManager.Create( createEntityMessage.EntityClass, createEntityMessage.EntityId );
	}

	private void HandleDeleteEntityMessage( NetworkMessage message )
	{
		if ( message is not DeleteEntityMessage deleteEntityMessage )
			return;
		
		SharedEntityManager.DeleteEntity( deleteEntityMessage.Entity );
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
				Logging.Error( "Got unexpected client state.", new ArgumentOutOfRangeException( nameof(clientStateChangedMessage.ClientState) ) );
				break;
		}
	}
		
	private void HandleClientPawnChangedMessage( NetworkMessage message )
	{
		if ( message is not ClientPawnChangedMessage clientPawnChangedMessage )
			return;

		if ( clientPawnChangedMessage.Client.Pawn is not null )
		{
			if ( clientPawnChangedMessage.Client == LocalClient )
				clientPawnChangedMessage.Client.Pawn.Changed -= OnLocalPawnChanged;
			clientPawnChangedMessage.Client.Pawn.Owner = null;
		}

		clientPawnChangedMessage.Client.Pawn = clientPawnChangedMessage.NewPawn;

		if ( clientPawnChangedMessage.Client.Pawn is not null )
		{
			if ( clientPawnChangedMessage.Client == LocalClient )
				clientPawnChangedMessage.Client.Pawn.Changed += OnLocalPawnChanged;
			clientPawnChangedMessage.Client.Pawn.Owner = clientPawnChangedMessage.Client;
		}
	}

	private void OnLocalPawnChanged( object? sender, EventArgs args )
	{
		_localPawnChanged = true;
	}
	
	private void HandleMultiEntityUpdateMessage( NetworkMessage message )
	{
		if ( message is not MultiEntityUpdateMessage entityUpdateMessage )
			return;

		var reader = new NetworkReader( new MemoryStream( entityUpdateMessage.PartialEntityData ) );
		var entityCount = reader.ReadInt32();
		for ( var i = 0; i < entityCount; i++ )
		{
			var entity = SharedEntityManager?.GetEntityById( reader.ReadInt32() );
			if ( entity is null )
			{
				Logging.Error( "Attempted to update an entity that does not exist.", new InvalidOperationException() );
				continue;
			}
		
			reader.ReadNetworkableChanges( entity );
		}
		reader.Close();
	}

	public void SendToServer( NetworkMessage message )
	{
		_outgoingQueue.Enqueue( message );
	}

	private void DispatchMessage( NetworkMessage message )
	{
#if DEBUG
		var messageType = message.GetType();
		if ( !MessageTypesReceived.ContainsKey( messageType ) )
			MessageTypesReceived.Add( messageType, 0 );
		MessageTypesReceived[messageType]++;
#endif

		if ( !_messageHandlers.TryGetValue( message.GetType(), out var cb ) )
		{
			Logging.Error( $"Unhandled message type {message.GetType()}.", new InvalidOperationException() );
			return;
		}
		
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
