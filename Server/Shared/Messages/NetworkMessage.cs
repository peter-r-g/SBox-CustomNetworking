using System;
using System.Collections.Generic;
using System.Linq;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared;

public abstract class NetworkMessage : INetworkable
{
	public event INetworkable.ChangedEventHandler? Changed;
	
	public abstract void Deserialize( NetworkReader reader );
	public void DeserializeChanges( NetworkReader reader )
	{
		throw new NotImplementedException();
	}

	public abstract void Serialize( NetworkWriter writer );
	public void SerializeChanges( NetworkWriter writer )
	{
		throw new NotImplementedException();
	}

	public static NetworkMessage DeserializeMessage( NetworkReader reader )
	{
		return reader.ReadNetworkable<NetworkMessage>();
	}

#if SERVER
	// TODO: Manually chunk everything
	public static PartialMessage[] Split( IEnumerable<byte> bytes )
	{
		var chunks = bytes.Chunk( SharedConstants.PartialMessagePayloadSize );
		var chunkCount = chunks.Count();
		var partialMessages = new PartialMessage[chunkCount];
		
		var messageGuid = Guid.NewGuid();
		var i = 0;
		foreach ( var chunk in chunks )
		{
			partialMessages[i] = new PartialMessage( messageGuid, chunkCount, i, chunk );
			i++;
		}

		return partialMessages;
	}
#endif
}
