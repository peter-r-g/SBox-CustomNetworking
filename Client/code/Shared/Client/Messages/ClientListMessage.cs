using System.Collections.Generic;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing a list of clients to notify the client about.
/// </summary>
public sealed class ClientListMessage : NetworkMessage
{
	/// <summary>
	/// Contains all client IDs to notify the client about.
	/// </summary>
	public ICollection<long> ClientIds { get; private set; }

#if SERVER
	public ClientListMessage( ICollection<long> clientIds )
	{
		ClientIds = clientIds;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		var list = new List<long> {Capacity = reader.ReadInt32()};
		for ( var i = 0; i < list.Capacity; i++ )
			list.Add( reader.ReadInt64() );

		ClientIds = list;
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( ClientIds.Count );
		foreach ( var playerId in ClientIds )
			writer.Write( playerId );
	}
}
