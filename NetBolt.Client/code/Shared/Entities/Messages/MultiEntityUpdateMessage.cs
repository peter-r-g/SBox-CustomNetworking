using System;
using NetBolt.Shared.Entities;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing information about an <see cref="IEntity"/> that has updated.
/// </summary>
public sealed class MultiEntityUpdateMessage : NetworkMessage
{
	/// <summary>
	/// Contains all data changes relating to entities.
	/// </summary>
	public byte[] PartialEntityData { get; private set; } = Array.Empty<byte>();

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
