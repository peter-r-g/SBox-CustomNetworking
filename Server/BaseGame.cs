using System;
using System.Collections.Generic;
using System.IO;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Server;

/// <summary>
/// The base class for any game servers.
/// </summary>
public class BaseGame
{
	/// <summary>
	/// The only instance of the game in existence.
	/// </summary>
	public static BaseGame Current = null!;

	/// <summary>
	/// A quick access to <see cref="NetworkServer"/>.<see cref="NetworkServer.Instance"/>.<see cref="NetworkServer.Clients"/>.
	/// </summary>
	public IReadOnlyDictionary<long, INetworkClient> Clients => NetworkServer.Instance.Clients;
	/// <summary>
	/// A quick access to <see cref="NetworkServer"/>.<see cref="NetworkServer.Instance"/>.<see cref="NetworkServer.Bots"/>.
	/// </summary>
	public IReadOnlyDictionary<long, BotClient> Bots => NetworkServer.Instance.Bots;
	
	/// <summary>
	/// Manages all server-side only entities.
	/// </summary>
	internal readonly EntityManager ServerEntityManager = new();
	/// <summary>
	/// Manages all networked entities.
	/// </summary>
	internal readonly EntityManager SharedEntityManager = new();

	/// <summary>
	/// The maximum tick rate of the server. In the event of severe performance hits the tick rate can drop below this desired number.
	/// </summary>
	protected virtual int TickRate => 60;

	/// <summary>
	/// Keeps track of all edited entities in the <see cref="SharedEntityManager"/>.
	/// </summary>
	private readonly HashSet<IEntity> _changedEntities = new();

	public BaseGame()
	{
		if ( Current is not null )
			throw new Exception( $"An instance of {nameof(BaseGame)} already exists." );
		
		Current = this;
	}

	/// <summary>
	/// Called at the start of the program. Use this to do a one-time startup of all needed things in your game.
	/// <remarks>At this point the networking server has not started.</remarks>
	/// </summary>
	public virtual void Start()
	{
		Program.TickRate = TickRate;
		NetworkServer.Instance.HandleMessage<RpcCallMessage>( Rpc.HandleRpcCallMessage );
		NetworkServer.Instance.HandleMessage<RpcCallResponseMessage>( Rpc.HandleRpcCallResponseMessage );

		SharedEntityManager.EntityCreated += OnNetworkedEntityCreated;
		SharedEntityManager.EntityDeleted += OnNetworkedEntityDeleted;
		SharedEntityManager.EntityChanged += OnNetworkedEntityChanged;
	}

	/// <summary>
	/// Called at the end of the program. Use this to cleanup any resources you have collected over the duration of running.
	/// <remarks>At this point the networking server is still running but do not expect it to send any messages at this point.</remarks>
	/// </summary>
	public virtual void Shutdown()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities )
			serverEntity.Delete();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities )
			sharedEntity.Delete();
	}

	/// <summary>
	/// Called at every tick of the program. Use this for your core game logic.
	/// <remarks>If overriding, it is highly recommended to call the base class method after your code. Otherwise, networked entities you have edited won't be sent to clients till the next tick.</remarks>
	/// </summary>
	public virtual void Update()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities )
			serverEntity.Update();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities )
			sharedEntity.Update();

		// TODO: PVS type system?
		if ( _changedEntities.Count == 0 )
			return;
		
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.Write( _changedEntities.Count );
		foreach ( var entity in _changedEntities )
		{
			writer.Write( entity.EntityId );
			entity.SerializeChanges( writer );
		}
		_changedEntities.Clear();
		writer.Close();
		
		NetworkServer.Instance.QueueMessage( To.All, new MultiEntityUpdateMessage( stream.ToArray() ) );
	}
	
	/// <summary>
	/// Called when a <see cref="INetworkClient"/> has been authorized and has joined the server.
	/// </summary>
	/// <param name="client">The handle of the client that has connected.</param>
	public virtual void OnClientConnected( INetworkClient client )
	{
		var toClient = To.Single( client );
		NetworkServer.Instance.QueueMessage( toClient, new ClientListMessage( NetworkServer.Instance.Clients.Values ) );
		NetworkServer.Instance.QueueMessage( toClient, new EntityListMessage( SharedEntityManager.Entities ) );
		NetworkServer.Instance.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Connected ) );
	}

	/// <summary>
	/// Called when a <see cref="INetworkClient"/> has disconnected from the server. This could be intentional or due to a timeout.
	/// </summary>
	/// <param name="client">The handle of the client that has disconnected.</param>
	public virtual void OnClientDisconnected( INetworkClient client )
	{
		var message = new ClientStateChangedMessage( client.ClientId, ClientState.Disconnected );
		NetworkServer.Instance.QueueMessage( To.AllExcept( client ), message );
	}
	
	/// <summary>
	/// Gets an <see cref="IEntity"/> that is local to the server.
	/// </summary>
	/// <param name="entityId">The ID of the <see cref="IEntity"/> to get.</param>
	/// <returns>The <see cref="IEntity"/> that was found. Null if no <see cref="IEntity"/> was found.</returns>
	public IEntity? GetLocalEntityById( int entityId )
	{
		return ServerEntityManager.GetEntityById( entityId );
	}

	/// <summary>
	/// Gets an <see cref="IEntity"/> that is available to both client and server.
	/// </summary>
	/// <param name="entityId">The ID of the <see cref="IEntity"/> to get.</param>
	/// <returns>The <see cref="IEntity"/> that was found. Null if no <see cref="IEntity"/> was found.</returns>
	public IEntity? GetNetworkedEntityById( int entityId )
	{
		return SharedEntityManager.GetEntityById( entityId );
	}

	protected virtual void OnNetworkedEntityCreated( IEntity entity )
	{
		NetworkServer.Instance.QueueMessage( To.All, new CreateEntityMessage( entity ) );
	}

	protected virtual void OnNetworkedEntityDeleted( IEntity entity )
	{
		NetworkServer.Instance.QueueMessage( To.All, new DeleteEntityMessage( entity ) );
	}
	
	protected virtual void OnNetworkedEntityChanged( IEntity entity )
	{
		_changedEntities.Add( entity );
	}
}
