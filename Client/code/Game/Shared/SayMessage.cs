using System.IO;
using CustomNetworking.Shared.Utility;
#if CLIENT
using CustomNetworking.Client;
#endif
#if SERVER
using CustomNetworking.Server;
#endif

namespace CustomNetworking.Shared.Messages;

public class SayMessage : NetworkMessage
{
	public INetworkClient? Sender { get; private set; }
	public string Message { get; private set; }

#if SERVER
	public SayMessage( INetworkClient? sender, string message )
	{
		Sender = sender;
		Message = message;
	}
	
	public SayMessage()
	{
		Sender = null;
		Message = string.Empty;
	}
#endif
	
#if CLIENT
	public SayMessage( string message )
	{
		Sender = null;
		Message = message;
	}
	
	public SayMessage()
	{
		Sender = null;
		Message = string.Empty;
	}
#endif

	public override void Deserialize( NetworkReader reader )
	{
#if SERVER
		Sender = NetworkManager.GetClientById( reader.ReadInt64() );
#endif
#if CLIENT
		Sender = NetworkManager.Instance?.GetClientById( reader.ReadInt64() );
#endif
		Message = reader.ReadString();
	}

	public override void Serialize( NetworkWriter writer )
	{
		if ( Sender is null )
			writer.Write( (long)-1 );
		else
			writer.Write( Sender.ClientId );
		writer.Write( Message );
	}
}
