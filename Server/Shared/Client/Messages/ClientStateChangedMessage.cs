using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class ClientStateChangedMessage : NetworkMessage
{
	public long ClientId { get; private set; }
	public ClientState ClientState { get; private set; }

#if SERVER
	public ClientStateChangedMessage( long clientId, ClientState clientState )
	{
		ClientId = clientId;
		ClientState = clientState;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		ClientId = reader.ReadInt64();
		ClientState = (ClientState)reader.ReadByte();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( ClientId );
		writer.Write( (byte)ClientState );
	}
}
