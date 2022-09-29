using System;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> to notify some clients when a client has had its pawn changed.
/// </summary>
public sealed class ClientPawnChangedMessage : NetworkMessage
{
	/// <summary>
	/// The <see cref="INetworkClient"/> that has had its <see cref="INetworkClient.Pawn"/> changed.
	/// </summary>
	public INetworkClient Client { get; private set; }
	/// <summary>
	/// The old <see cref="IEntity"/> the <see cref="Client"/> was controlling.
	/// </summary>
	public IEntity? OldPawn { get; private set; }
	/// <summary>
	/// The new <see cref="IEntity"/> the <see cref="Client"/> is controlling.
	/// </summary>
	public IEntity? NewPawn { get; private set; }
	
#if SERVER
	public ClientPawnChangedMessage( INetworkClient client, IEntity? oldEntity, IEntity? newEntity )
	{
		Client = client;
		OldPawn = oldEntity;
		NewPawn = newEntity;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		var clientId = reader.ReadInt64();
		if ( !INetworkClient.All.TryGetValue( clientId, out var client ) )
		{
			Logging.Error( $"Failed to get client with ID \"{clientId}\"", new InvalidOperationException() );
			return;
		}

		Client = client;
		if ( reader.ReadBoolean() )
			OldPawn = IEntity.All.GetEntityById( reader.ReadInt32() );
		if ( reader.ReadBoolean() )
			NewPawn = IEntity.All.GetEntityById( reader.ReadInt32() );
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( Client.ClientId );
		
		var hasOldPawn = OldPawn is not null;
		writer.Write( hasOldPawn );
		if ( hasOldPawn )
			writer.Write( OldPawn!.EntityId );

		var hasNewPawn = NewPawn is not null;
		writer.Write( hasNewPawn );
		if ( hasNewPawn )
			writer.Write( NewPawn!.EntityId );
	}
}
