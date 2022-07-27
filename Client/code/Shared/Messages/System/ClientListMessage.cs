using System.Collections.Generic;
using System.IO;

namespace CustomNetworking.Shared.Messages;

public class ClientListMessage : NetworkMessage
{
	public readonly ICollection<long> ClientIds;

#if SERVER
	public ClientListMessage( ICollection<long> clientIds )
	{
		ClientIds = clientIds;
	}
#endif

#if CLIENT
	public ClientListMessage( BinaryReader reader )
	{
		var list = new List<long> {Capacity = reader.ReadInt32()};
		for ( var i = 0; i < list.Capacity; i++ )
			list.Add( reader.ReadInt64() );

		ClientIds = list;
	}
#endif
	
	public override void Serialize( BinaryWriter writer )
	{
		writer.Write( nameof(ClientListMessage) );
		writer.Write( ClientIds.Count );
		foreach ( var playerId in ClientIds )
			writer.Write( playerId );
	}
}
