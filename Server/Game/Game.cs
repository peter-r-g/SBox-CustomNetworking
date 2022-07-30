using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Server.Shared.Messages;
using CustomNetworking.Server.Shared.Rpc;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Game;

public class Game
{
	private const int BotLimit = 0;

	public readonly EntityManager ServerEntityManager = new();
	public readonly EntityManager SharedEntityManager = new();

	private GameInformationEntity? _gameInformationEntity;
	private readonly HashSet<IEntity> _changedEntities = new();

	public void Start()
	{
		Program.TickRate = 60;
		Program.Server.HandleMessage<RpcCallMessage>( HandleRpcCallMessage );
		Program.Server.HandleMessage<RpcCallResponseMessage>( HandleRpcCallResponseMessage );
		Program.Server.HandleMessage<SayMessage>( HandleSayMessage );
		
		BotClient.HandleBotMessage<PartialMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<ShutdownMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<ClientListMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<EntityListMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<ClientStateChangedMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<EntityUpdateMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<RpcCallMessage>( DumpBotMessage );
		
		BotClient.HandleBotMessage<SayMessage>( DumpBotMessage );
		
		Task.Run( AddBotLoopAsync );
		
		SharedEntityManager.EntityChanged += SharedEntityChanged;

		_gameInformationEntity = SharedEntityManager.Create<GameInformationEntity>();
		for ( var i = 0; i < 100; i++ )
			_gameInformationEntity.TestItems.Add( i );
		
		for ( var i = 0; i < 5; i++ )
			SharedEntityManager.Create<TestCitizenEntity>();
	}

	public void Shutdown()
	{
	}

	public void Update()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities )
			serverEntity.Update();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities )
			sharedEntity.Update();

		foreach ( var entity in _changedEntities )
			Program.Server.QueueMessage( To.All, new EntityUpdateMessage( entity ) );
		_changedEntities.Clear();
	}

	public void OnClientConnected( INetworkClient client )
	{
		var toClient = To.Single( client );
		Program.Server.QueueMessage( toClient, new ClientListMessage( Program.Server.Clients.Keys ) );
		Program.Server.QueueMessage( toClient, new EntityListMessage( SharedEntityManager.Entities ) );
		Program.Server.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Connected ) );
		
		Program.Logger.Enqueue( $"{client.ClientId} has connected" );
	}

	public void OnClientDisconnected( INetworkClient client )
	{
		var message = new ClientStateChangedMessage( client.ClientId, ClientState.Disconnected );
		Program.Server.QueueMessage( To.AllExcept( client ), message );
		
		Program.Logger.Enqueue( $"{client.ClientId} has disconnected" );
	}
	
	private void SharedEntityChanged( INetworkable entity )
	{
		_changedEntities.Add( (entity as IEntity)! );
	}
	
	private static async Task AddBotLoopAsync()
	{
		while ( !Program.ProgramCancellation.IsCancellationRequested && Program.Server.Bots.Count < BotLimit )
		{
			Program.Server.AcceptClient( Math.Abs( Random.Shared.NextInt64() ) );
			await Task.Delay( 1 );
		}
	}

	private void HandleRpcCallMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not RpcCallMessage rpcCall )
			return;

		var type = TypeHelper.GetTypeByName( rpcCall.ClassName );
		if ( type is null )
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.ClassName}\" doesn't exist in the current assembly)." );

		var method = type.GetMethod( rpcCall.MethodName );
		if ( method is null )
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.MethodName}\" does not exist on \"{type}\")." );

		if ( method.GetCustomAttribute( typeof(Rpc.ServerAttribute) ) is null )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to invoke a non-RPC method)." );
		
		var instance = SharedEntityManager.GetEntityById( rpcCall.EntityId );
		if ( instance is null && rpcCall.EntityId != -1 )
			throw new InvalidOperationException(
				"Failed to handle RPC call (Attempted to call RPC on a non-existant entity)." );

		var returnValue = method.Invoke( instance, rpcCall.Parameters );
		if ( rpcCall.CallGuid == Guid.Empty )
			return;

		if ( returnValue is not INetworkable && returnValue is not null )
		{
			var failedMessage = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Failed );
			Program.Server.QueueMessage( To.Single( client ), failedMessage );
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.MethodName}\" returned a non-networkable value)." );
		}

		var response = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Completed,
			returnValue as INetworkable ?? null );
		Program.Server.QueueMessage( To.Single( client ), response );
	}

	private void HandleRpcCallResponseMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not RpcCallResponseMessage rpcResponse )
			return;

		if ( !Rpc.RpcResponses.TryAdd( rpcResponse.CallGuid, rpcResponse ) )
			throw new InvalidOperationException( $"Failed to handle RPC call response (Failed to add \"{rpcResponse.CallGuid}\" to response dictionary)." );
	}

	private static void HandleSayMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not SayMessage sayMessage )
			return;
		
		Program.Server.QueueMessage( To.AllExcept( client ), new SayMessage( client, sayMessage.Message ) );
		Program.Logger.Enqueue( $"{client.ClientId}: {sayMessage.Message}" );
	}

	private static void DumpBotMessage( BotClient client, NetworkMessage message )
	{
	}
}
