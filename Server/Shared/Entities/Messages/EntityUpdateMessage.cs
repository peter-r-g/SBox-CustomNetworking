#if CLIENT
using System.Buffers;
#endif
using System.IO;
using CustomNetworking.Shared.Utility;
#if SERVER
using CustomNetworking.Shared.Entities;
#endif

namespace CustomNetworking.Shared.Messages;

public sealed class EntityUpdateMessage : NetworkMessage
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

#if CLIENT
	~EntityUpdateMessage()
	{
		ArrayPool<byte>.Shared.Return( EntityData, true );
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
#if CLIENT
		EntityId = reader.ReadInt32();
		var dataLength = reader.ReadInt32();
		EntityData = ArrayPool<byte>.Shared.Rent( dataLength );
		_ = reader.Read( EntityData, 0, dataLength );
#endif
	}
	
	public override void Serialize( NetworkWriter writer )
	{
#if SERVER
		writer.Write( EntityId );
		writer.Write( EntityData.Length );
		writer.Write( EntityData );
#endif
	}
}
