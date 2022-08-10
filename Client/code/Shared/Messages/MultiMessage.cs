using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A <see cref="NetworkMessage"/> that contains multiple <see cref="NetworkMessage"/>s in itself.
/// </summary>
public sealed class MultiMessage : NetworkMessage
{
	/// <summary>
	/// The <see cref="NetworkMessage"/>s to send.
	/// </summary>
	public NetworkMessage[] Messages { get; private set; }
	
#if SERVER
	public MultiMessage( params NetworkMessage[] messages )
	{
		Messages = messages;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		Messages = new NetworkMessage[reader.ReadInt32()];
		for ( var i = 0; i < Messages.Length; i++ )
			Messages[i] = reader.ReadNetworkable<NetworkMessage>();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( Messages.Length );
		foreach ( var message in Messages )
			writer.WriteNetworkable<NetworkMessage>( message );
	}
}
