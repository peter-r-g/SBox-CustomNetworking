using System.IO;
#if CLIENT
using CustomNetworking.Client;
#endif
#if SERVER
using CustomNetworking.Server;
#endif

namespace CustomNetworking.Shared.Messages;

public class SayMessage : NetworkMessage
{
	public INetworkClient? Sender { get; }
	public string Message { get; }

#if SERVER
	public SayMessage( INetworkClient? sender, string message )
	{
		Sender = sender;
		Message = message;
	}
#endif
	
#if CLIENT
	public SayMessage( string message )
	{
		Sender = null;
		Message = message;
	}
#endif

	public SayMessage( BinaryReader reader )
	{
#if SERVER
		Sender = NetworkManager.GetClientById( reader.ReadInt64() );
#endif
#if CLIENT
		Sender = NetworkManager.Instance?.GetClientById( reader.ReadInt64() );
#endif
		Message = reader.ReadString();
	}

	public override void Serialize( BinaryWriter writer )
	{
		writer.Write( nameof(SayMessage) );
		if ( Sender is null )
			writer.Write( (long)-1 );
		else
			writer.Write( Sender.ClientId );
		writer.Write( Message );
	}
}
