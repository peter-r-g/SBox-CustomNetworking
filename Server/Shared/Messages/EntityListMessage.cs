#if CLIENT
using System.Buffers;
#endif
using System.Collections.Generic;
using System.IO;
using CustomNetworking.Shared.Utility;
#if SERVER
using CustomNetworking.Shared.Entities;
#endif

namespace CustomNetworking.Shared.Messages;

public class EntityListMessage : NetworkMessage
{
	public List<byte[]> EntityData { get; private set; }

#if SERVER
	public EntityListMessage( IEnumerable<IEntity> entityList )
	{
		var entityData = new List<byte[]>();

		foreach ( var entity in entityList )
		{
			var stream = new MemoryStream();
			var writer = new NetworkWriter( stream );
			writer.WriteNetworkable( entity );
			writer.Close();
			entityData.Add( stream.ToArray() );
		}
		
		EntityData = entityData;
	}
#endif

#if CLIENT
	~EntityListMessage()
	{
		foreach ( var data in EntityData )
			ArrayPool<byte>.Shared.Return( data, true );
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		EntityData = new List<byte[]> {Capacity = reader.ReadInt32()};
		for ( var i = 0; i < EntityData.Capacity; i++ )
		{
			var dataLength = reader.ReadInt32();
			var bytes = ArrayPool<byte>.Shared.Rent( dataLength );
			_ = reader.Read( bytes, 0, dataLength );
			EntityData.Add( bytes );
		}
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityData.Count );
		foreach ( var data in EntityData )
		{
			writer.Write( data.Length );
			writer.Write( data );
		}
	}
}
