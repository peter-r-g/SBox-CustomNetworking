#if CLIENT
using System.Buffers;
#endif
using System.Collections.Generic;
using NetBolt.Shared.Entities;
#if SERVER
using System.IO;
#endif
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing a list of <see cref="IEntity"/>s to notify the client about.
/// </summary>
public sealed class EntityListMessage : NetworkMessage
{
	/// <summary>
	/// The data of all <see cref="IEntity"/>s passed.
	/// </summary>
	public List<byte[]> EntityData { get; private set; } = new();

#if SERVER
	public EntityListMessage( IEnumerable<IEntity> entityList )
	{
		EntityData = new List<byte[]>();
		foreach ( var entity in entityList )
		{
			var stream = new MemoryStream();
			var writer = new NetworkWriter( stream );
			writer.WriteEntity( entity );
			writer.Close();
			EntityData.Add( stream.ToArray() );
		}
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
#if CLIENT
		EntityData = new List<byte[]> { Capacity = reader.ReadInt32() };
		for ( var i = 0; i < EntityData.Capacity; i++ )
		{
			var dataLength = reader.ReadInt32();
			var bytes = ArrayPool<byte>.Shared.Rent( dataLength );
			_ = reader.Read( bytes, 0, dataLength );
			EntityData.Add( bytes );
		}
#endif
	}

	public override void Serialize( NetworkWriter writer )
	{
#if SERVER
		writer.Write( EntityData.Count );
		foreach ( var data in EntityData )
		{
			writer.Write( data.Length );
			writer.Write( data );
		}
#endif
	}
}
