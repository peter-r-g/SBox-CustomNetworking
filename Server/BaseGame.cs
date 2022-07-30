using System;
using System.Collections.Generic;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Server;

public class BaseGame
{
	public static BaseGame Current = null!;

	public List<IEntity> LocalEntities => ServerEntityManager.Entities;
	public List<IEntity> NetworkedEntities => SharedEntityManager.Entities;
	protected virtual int TickRate => 60;
	
	protected readonly EntityManager ServerEntityManager = new();
	protected readonly EntityManager SharedEntityManager = new();
	
	private readonly HashSet<IEntity> _changedEntities = new();

	public BaseGame()
	{
		if ( Current is not null )
			throw new Exception( $"An instance of {nameof(BaseGame)} already exists." );
		
		Current = this;
	}

	public void Start()
	{
		Program.TickRate = TickRate;
		NetworkServer.Instance.HandleMessage<RpcCallMessage>( Rpc.HandleRpcCallMessage );
		NetworkServer.Instance.HandleMessage<RpcCallResponseMessage>( Rpc.HandleRpcCallResponseMessage );
		
		SharedEntityManager.EntityChanged += OnNetworkedEntityChanged;
	}

	public virtual void Shutdown()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities )
			serverEntity.Delete();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities )
			sharedEntity.Delete();
	}

	public virtual void Update()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities )
			serverEntity.Update();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities )
			sharedEntity.Update();

		foreach ( var entity in _changedEntities )
			NetworkServer.Instance.QueueMessage( To.All, new EntityUpdateMessage( entity ) );
		_changedEntities.Clear();
	}
	
	public virtual void OnClientConnected( INetworkClient client )
	{
		var toClient = To.Single( client );
		NetworkServer.Instance.QueueMessage( toClient, new ClientListMessage( NetworkServer.Instance.Clients.Keys ) );
		NetworkServer.Instance.QueueMessage( toClient, new EntityListMessage( SharedEntityManager.Entities ) );
		NetworkServer.Instance.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Connected ) );
	}

	public virtual void OnClientDisconnected( INetworkClient client )
	{
		var message = new ClientStateChangedMessage( client.ClientId, ClientState.Disconnected );
		NetworkServer.Instance.QueueMessage( To.AllExcept( client ), message );
	}
	
	public IEntity? GetLocalEntityById( int entityId )
	{
		return ServerEntityManager.GetEntityById( entityId );
	}

	public IEntity? GetNetworkedEntityById( int entityId )
	{
		return SharedEntityManager.GetEntityById( entityId );
	}
	
	private void OnNetworkedEntityChanged( INetworkable entity )
	{
		_changedEntities.Add( (entity as IEntity)! );
	}
}
