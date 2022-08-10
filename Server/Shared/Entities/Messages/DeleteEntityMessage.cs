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
	public IEntity Entity { get; private set; }
	
#if SERVER
	public DeleteEntityMessage( IEntity entity )
	{
		Entity = entity;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		var entity = IEntity.All.GetEntityById( reader.ReadInt32() );
		Entity = entity ?? throw new InvalidOperationException( "Attempted to delete an entity that does not exist on the client." );
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( Entity.EntityId );
	}
}
