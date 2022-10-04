using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> containing a client ID and the state it is now in.
/// </summary>
public sealed class ClientStateChangedMessage : NetworkMessage
{
	/// <summary>
	/// The ID of the client that has changed.
	/// </summary>
	public long ClientId { get; private set; }
	/// <summary>
	/// The new state of the client.
	/// </summary>
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
