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
	/// <summary>
	/// Contains all data changes relating to entities.
	/// </summary>
	public byte[] PartialEntityData { get; private set; }
	
#if SERVER
	public MultiEntityUpdateMessage( byte[] partialEntityData )
	{
		PartialEntityData = partialEntityData;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
#if CLIENT
		PartialEntityData = new byte[reader.ReadInt32()];
		_ = reader.Read( PartialEntityData, 0, PartialEntityData.Length );
#endif
	}
	
	public override void Serialize( NetworkWriter writer )
	{
#if SERVER
		writer.Write( PartialEntityData.Length );
		writer.Write( PartialEntityData );
#endif
	}
}
