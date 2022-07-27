using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomNetworking.Shared.Messages;
#if SERVER
using CustomNetworking.Server;
#endif

namespace CustomNetworking.Shared;

public abstract class NetworkMessage
{
	public const string MessagesNamespace = "CustomNetworking.Shared.Messages";
	
	public abstract void Serialize( BinaryWriter writer );

	public static NetworkMessage Deserialize( BinaryReader reader )
	{
		var typeName = reader.ReadString();
#if CLIENT
		return TypeLibrary.Create<NetworkMessage>( TypeLibrary.GetTypeByName( typeName ), new object[] {reader} );
#endif
#if SERVER
		typeName = MessagesNamespace + "." + typeName;
		var messageType = Type.GetType( typeName );
		if ( messageType is null )
			throw new InvalidOperationException( $"Failed to create instance of message (\"{typeName}\" does not exist in the current assembly)." );
		
		var message = (NetworkMessage)Activator.CreateInstance( messageType, reader )!;
		if ( message is null )
			throw new InvalidOperationException( $"Failed to create instance of message (instance creation failed)" );

		return message;
#endif
	}

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
}
