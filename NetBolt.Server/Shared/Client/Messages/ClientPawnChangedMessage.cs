using System;
using System.Linq;
using NetBolt.Shared.Entities;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> to notify some clients when a client has had its pawn changed.
/// </summary>
public sealed class ClientPawnChangedMessage : NetworkMessage
{
	/// <summary>
	/// The <see cref="INetworkClient"/> that has had its <see cref="INetworkClient.Pawn"/> changed.
	/// </summary>
	public INetworkClient Client { get; private set; } = null!;
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
		var client = INetworkClient.All.FirstOrDefault( client => client.ClientId == clientId );
		if ( client is null )
		{
			Logging.Error( $"Failed to get client with ID \"{clientId}\"" );
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
