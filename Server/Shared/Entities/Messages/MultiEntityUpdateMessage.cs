using CustomNetworking.Shared.Utility;
#if SERVER
using CustomNetworking.Shared.Entities;
#endif

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing information about an <see cref="IEntity"/> that has updated.
/// </summary>
public sealed class MultiEntityUpdateMessage : NetworkMessage
{
	public byte[] EntityData { get; private set; }
	
#if SERVER
	public MultiEntityUpdateMessage( byte[] entityData )
	{
		EntityData = entityData;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
#if CLIENT
		EntityData = new byte[reader.ReadInt32()];
		_ = reader.Read( EntityData, 0, EntityData.Length );
#endif
	}
	
	public override void Serialize( NetworkWriter writer )
	{
#if SERVER
		writer.Write( EntityData.Length );
		writer.Write( EntityData );
#endif
	}
}
