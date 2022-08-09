using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class DeleteEntityMessage : NetworkMessage
{
	public int EntityId { get; private set; }
	
#if SERVER
	public DeleteEntityMessage( IEntity entity )
	{
		EntityId = entity.EntityId;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		EntityId = reader.ReadInt32();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityId );
	}
}
