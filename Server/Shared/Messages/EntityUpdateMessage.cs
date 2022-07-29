using System.IO;
using CustomNetworking.Shared.Utility;
#if SERVER
using CustomNetworking.Shared.Entities;
#endif

namespace CustomNetworking.Shared.Messages;

public class EntityUpdateMessage : NetworkMessage
{
	public int EntityId { get; private set; }
	public byte[] EntityData { get; private set; }
	
#if SERVER
	public EntityUpdateMessage( IEntity entity )
	{
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.Write( entity.EntityId );
		writer.WriteNetworkableChanges( entity );
		writer.Close();

		EntityData = stream.ToArray();
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		EntityId = reader.ReadInt32();
		var dataLength = reader.ReadInt32();
		var bytes = new byte[dataLength];
		_ = reader.Read( bytes, 0, dataLength );
		EntityData = bytes;
	}
	
	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityId );
		writer.Write( EntityData.Length );
		writer.Write( EntityData );
	}
}
