using System;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class ClientPawnChangedMessage : NetworkMessage
{
	public INetworkClient Client { get; private set; }
	public IEntity? OldPawn { get; private set; }
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
			throw new Exception( $"Failed to get client with ID \"{clientId}\"" );

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
