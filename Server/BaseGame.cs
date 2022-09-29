using System;
using System.IO;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.RemoteProcedureCalls;
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

	public BaseGame()
	{
		if ( Current is not null )
			Logging.Fatal( new InvalidOperationException( $"An instance of {nameof(BaseGame)} already exists." ) );
		
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
		NetworkServer.Instance.HandleMessage<ClientPawnUpdateMessage>( HandleClientPawnUpdateMessage );

		SharedEntityManager.EntityCreated += OnNetworkedEntityCreated;
		SharedEntityManager.EntityDeleted += OnNetworkedEntityDeleted;
	}

	/// <summary>
	/// Called at the end of the program. Use this to cleanup any resources you have collected over the duration of running.
	/// <remarks>At this point the networking server is still running but do not expect it to send any messages at this point.</remarks>
	/// </summary>
	public virtual void Shutdown()
	{
		ServerEntityManager.DeleteAllEntities();	
		SharedEntityManager.DeleteAllEntities();
	}

	/// <summary>
	/// Called at every tick of the program. Use this for your core game logic.
	/// <remarks>If overriding, it is highly recommended to call the base class method after your code. Otherwise, networked entities you have edited won't be sent to clients till the next tick.</remarks>
	/// </summary>
	public virtual void Update()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities.Values )
			serverEntity.Update();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities.Values )
			sharedEntity.Update();

		// TODO: PVS type system?
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		var countPos = writer.BaseStream.Position;
		writer.BaseStream.Position += sizeof(int);

		var count = 0;
		foreach ( var entity in SharedEntityManager.Entities.Values )
		{
			if ( !entity.Changed() )
				continue;
			
			count++;
			writer.Write( entity.EntityId );
			entity.SerializeChanges( writer );
		}

		var tempPos = writer.BaseStream.Position;
		writer.BaseStream.Position = countPos;
		writer.Write( count );
		writer.BaseStream.Position = tempPos;
		writer.Close();
		
		if ( count != 0 )
			NetworkServer.Instance.QueueMessage( To.All, new MultiEntityUpdateMessage( stream.ToArray() ) );
	}
	
	/// <summary>
	/// Called when a <see cref="INetworkClient"/> has been authorized and has joined the server.
	/// </summary>
	/// <param name="client">The handle of the client that has connected.</param>
	public virtual void OnClientConnected( INetworkClient client )
	{
		Logging.Info( $"{client} has connected" );
		
		var toClient = To.Single( client );
		NetworkServer.Instance.QueueMessage( toClient, new ClientListMessage( NetworkServer.Instance.Clients.Values ) );
		NetworkServer.Instance.QueueMessage( toClient, new EntityListMessage( SharedEntityManager.Entities.Values ) );
		NetworkServer.Instance.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Connected ) );
		
		client.PawnChanged += ClientOnPawnChanged;
		client.Pawn = SharedEntityManager.Create<BasePlayer>();
		client.Pawn.Owner = client;
	}

	/// <summary>
	/// Called when a <see cref="INetworkClient"/> has disconnected from the server. This could be intentional or due to a timeout.
	/// </summary>
	/// <param name="client">The handle of the client that has disconnected.</param>
	public virtual void OnClientDisconnected( INetworkClient client )
	{
		Logging.Info( $"{client} has disconnected" );
		
		NetworkServer.Instance.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Disconnected ) );
		if ( client.Pawn is not null )
			SharedEntityManager.DeleteEntity( client.Pawn );
		client.PawnChanged -= ClientOnPawnChanged;
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

	/// <summary>
	/// Called when an <see cref="IEntity"/> is created in the <see cref="SharedEntityManager"/>.
	/// </summary>
	/// <param name="entity">The <see cref="IEntity"/> that has been created.</param>
	protected virtual void OnNetworkedEntityCreated( IEntity entity )
	{
		NetworkServer.Instance.QueueMessage( To.All, new CreateEntityMessage( entity ) );
	}

	/// <summary>
	/// Called when an <see cref="IEntity"/> is deleted in the <see cref="SharedEntityManager"/>.
	/// </summary>
	/// <param name="entity">The <see cref="IEntity"/> that has been deleted.</param>
	protected virtual void OnNetworkedEntityDeleted( IEntity entity )
	{
		NetworkServer.Instance.QueueMessage( To.All, new DeleteEntityMessage( entity ) );
	}
	
	/// <summary>
	/// Called when a <see cref="INetworkClient"/>s pawn has been swapped.
	/// </summary>
	/// <param name="client">The <see cref="INetworkClient"/> that has its pawn changed.</param>
	/// <param name="oldpawn">The old <see cref="IEntity"/> the <see cref="client"/> was controlling.</param>
	/// <param name="newPawn">The new <see cref="IEntity"/> the <see cref="client"/> is now controlling.</param>
	protected virtual void ClientOnPawnChanged( INetworkClient client, IEntity? oldpawn, IEntity? newPawn )
	{
		NetworkServer.Instance.QueueMessage( To.All, new ClientPawnChangedMessage( client, oldpawn, newPawn ) );
	}

	/// <summary>
	/// Called when a <see cref="INetworkClient"/>s <see cref="INetworkClient.Pawn"/> has updated and the server needs to process it.
	/// </summary>
	/// <param name="client">The <see cref="INetworkClient"/> that sent this update.</param>
	/// <param name="message">The <see cref="NetworkMessage"/> of the update.</param>
	protected virtual void HandleClientPawnUpdateMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not ClientPawnUpdateMessage clientPawnUpdateMessage )
			return;

		if ( client.Pawn is null )
		{
			Logging.Error( $"Received a {nameof(ClientPawnUpdateMessage)} when the client has no pawn." );
			return;
		}
		
		var reader = new NetworkReader( new MemoryStream( clientPawnUpdateMessage.PartialPawnData ) );
		client.Pawn.DeserializeChanges( reader );
		reader.Close();
	}
}
