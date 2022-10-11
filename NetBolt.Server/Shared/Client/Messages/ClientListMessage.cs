using System.Collections.Generic;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing a list of <see cref="INetworkClient"/>s to notify the client about.
/// </summary>
public sealed class ClientListMessage : NetworkMessage
{
	/// <summary>
	/// Contains all client IDs to notify the client about.
	/// </summary>
	public List<(long, int)> ClientIds { get; private set; } = null!;

#if SERVER
	public ClientListMessage( IReadOnlyCollection<INetworkClient> clients )
	{
		ClientIds = new List<(long, int)> { Capacity = clients.Count };
		foreach ( var client in clients )
			ClientIds.Add( (client.ClientId, client.Pawn?.EntityId ?? -1) );
	}
#endif

	public override void Deserialize( NetworkReader reader )
	{
		var list = new List<(long, int)> { Capacity = reader.ReadInt32() };
		for ( var i = 0; i < list.Capacity; i++ )
			list.Add( (reader.ReadInt64(), reader.ReadInt32()) );

		ClientIds = list;
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( ClientIds.Count );
		foreach ( var pair in ClientIds )
		{
			writer.Write( pair.Item1 );
			writer.Write( pair.Item2 );
		}
	}
}
