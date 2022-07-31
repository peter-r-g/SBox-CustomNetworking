using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class CreateEntityMessage : NetworkMessage
{
	public string EntityClass { get; private set; }
	public int EntityId { get; private set; }

#if SERVER
	public CreateEntityMessage( string entityClass, int entityId )
	{
		EntityClass = entityClass;
		EntityId = entityId;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		EntityClass = reader.ReadString();
		EntityId = reader.ReadInt32();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityClass );
		writer.Write( EntityId );
	}
}
