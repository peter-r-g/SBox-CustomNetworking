#if CLIENT
using System.Buffers;
#endif
using System.IO;
using CustomNetworking.Shared.Utility;
#if SERVER
using CustomNetworking.Shared.Entities;
#endif

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing information about an <see cref="IEntity"/> that has updated.
/// </summary>
public sealed class EntityUpdateMessage : NetworkMessage
{
	/// <summary>
	/// The unique identifier of the <see cref="IEntity"/> that has been updated.
	/// </summary>
	public int EntityId { get; private set; }
	/// <summary>
	/// The changed data of the <see cref="IEntity"/>.
	/// </summary>
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
