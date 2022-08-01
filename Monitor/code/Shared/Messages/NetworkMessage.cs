using System;
#if SERVER
using System.Collections.Generic;
using System.Linq;
using CustomNetworking.Shared.Messages;
#endif
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared;

/// <summary>
/// Base class for sending any information between a client and a server.
/// </summary>
public abstract class NetworkMessage : INetworkable
{
	public event INetworkable.ChangedEventHandler? Changed;
	
	public abstract void Deserialize( NetworkReader reader );
	public void DeserializeChanges( NetworkReader reader )
	{
		Deserialize( reader );
	}

	public abstract void Serialize( NetworkWriter writer );
	public void SerializeChanges( NetworkWriter writer )
	{
		Serialize( writer );
	}

	public static NetworkMessage DeserializeMessage( NetworkReader reader )
	{
		return reader.ReadNetworkable<NetworkMessage>();
	}

#if SERVER
	// TODO: Manually chunk everything
	/// <summary>
	/// Splits up a <see cref="NetworkMessage"/> into <see cref="PartialMessage"/>s that can be sent without exceeding <see cref="SharedConstants"/>.<see cref="SharedConstants.MaxBufferSize"/>.
	/// </summary>
	/// <param name="bytes">The bytes to split.</param>
	/// <returns>An array of the created partial messages.</returns>
	public static PartialMessage[] Split( IEnumerable<byte> bytes )
	{
		var chunks = bytes.Chunk( PartialMessage.MaxPayloadSize );
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
