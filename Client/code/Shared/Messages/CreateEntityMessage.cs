using System.IO;

namespace CustomNetworking.Shared.Messages;

public class CreateEntityMessage : NetworkMessage
{
	public readonly string EntityClass;
	public readonly int EntityId;

#if SERVER
	public CreateEntityMessage( string entityClass, int entityId )
	{
		EntityClass = entityClass;
		EntityId = entityId;
	}
#endif

#if CLIENT
	public CreateEntityMessage( BinaryReader reader )
	{
		EntityClass = reader.ReadString();
		EntityId = reader.ReadInt32();
	}
#endif
	
	public override void Serialize( BinaryWriter writer )
	{
		writer.Write( nameof(CreateEntityMessage) );
		writer.Write( EntityClass );
		writer.Write( EntityId );
	}
}
