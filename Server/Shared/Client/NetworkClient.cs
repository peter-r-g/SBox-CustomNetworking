
using CustomNetworking.Shared.Entities;
#if SERVER
using System.IO;
using CustomNetworking.Server;
#endif

namespace CustomNetworking.Shared;

public class NetworkClient : INetworkClient
{
	public long ClientId { get; }
	public NetworkEntity Pawn { get; set; }
	
#if SERVER
	public ClientSocket ClientSocket { get; }
#endif
	
#if SERVER
	public NetworkClient( long clientId, ClientSocket clientSocket )
	{
		ClientId = clientId;
		ClientSocket = clientSocket;
		clientSocket.OnDataReceived += OnDataReceived;
		clientSocket.OnMessageReceived += OnMessageReceived;
	}

	~NetworkClient()
	{
		ClientSocket.OnDataReceived -= OnDataReceived;
		ClientSocket.OnMessageReceived -= OnMessageReceived;
	}
#endif

#if CLIENT
	public NetworkClient( long clientId )
	{
		ClientId = clientId;
	}
#endif

	public void SendMessage( byte[] bytes )
	{
#if SERVER
		ClientSocket.Send( bytes );
#endif
	}
	
	public void SendMessage( NetworkMessage message )
	{
#if SERVER
		var stream = new MemoryStream();
		var writer = new BinaryWriter( stream );
		message.Serialize( writer );
		writer.Close();
		
		ClientSocket.Send( stream.ToArray() );
#endif
	}

#if SERVER
	protected virtual void OnDataReceived( MemoryStream stream )
	{
		var reader = new BinaryReader( stream );
		var message = NetworkMessage.Deserialize( reader );
		reader.Close();
		
		NetworkManager.QueueIncoming( this, message );
	}

	protected virtual void OnMessageReceived( string message )
	{
	}
#endif
}
