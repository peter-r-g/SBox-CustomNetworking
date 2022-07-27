using System.IO;

namespace CustomNetworking.Shared.Messages;

public class ClientStateChangedMessage : NetworkMessage
{
	public readonly long ClientId;
	public readonly ClientState ClientState;

#if SERVER
	public ClientStateChangedMessage( long clientId, ClientState clientState )
	{
		ClientId = clientId;
		ClientState = clientState;
	}
#endif

#if CLIENT
	public ClientStateChangedMessage( BinaryReader reader )
	{
		ClientId = reader.ReadInt64();
		ClientState = (ClientState)reader.ReadByte();
	}
#endif
	
	public override void Serialize( BinaryWriter writer )
	{
		writer.Write( nameof(ClientStateChangedMessage) );
		writer.Write( ClientId );
		writer.Write( (byte)ClientState );
	}
}
