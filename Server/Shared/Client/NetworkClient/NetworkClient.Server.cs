#if SERVER
using System.IO;
using CustomNetworking.Server;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared;

public partial class NetworkClient
{
	internal ClientSocket ClientSocket { get; }
	
	internal NetworkClient( long clientId, ClientSocket clientSocket )
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
	
	public void SendMessage( byte[] bytes )
	{
#if DEBUG
		NetworkServer.Instance.MessagesSentToClients++;
#endif
		ClientSocket.Send( bytes );
	}
	
	public void SendMessage( NetworkMessage message )
	{
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkable( message );
		writer.Close();
		
		SendMessage( stream.ToArray() );
	}
	
	protected virtual void OnDataReceived( MemoryStream stream )
	{
		var reader = new NetworkReader( stream );
		var message = NetworkMessage.DeserializeMessage( reader );
		reader.Close();
		
		NetworkServer.Instance.QueueIncoming( this, message );
	}

	protected virtual void OnMessageReceived( string message )
	{
	}
}
#endif
