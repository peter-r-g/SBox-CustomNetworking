using System;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> that contains an <see cref="IEntity"/> to delete.
/// </summary>
public sealed class DeleteEntityMessage : NetworkMessage
{
	/// <summary>
	/// The <see cref="IEntity"/> to delete.
	/// </summary>
	public IEntity Entity { get; private set; } = null!;
	
#if SERVER
	public DeleteEntityMessage( IEntity entity )
	{
		Entity = entity;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		var entityId = reader.ReadInt32();
		var entity = IEntity.All.GetEntityById( entityId );
		if ( entity is null )
		{
			Logging.Error( $"Attempted to delete entity \"{entityId}\" which does not exist." );
			return;
		}

		Entity = entity;
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( Entity.EntityId );
	}
}
