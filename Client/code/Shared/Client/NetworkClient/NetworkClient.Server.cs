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
		NetworkServer.Instance.MessagesSentToClients++;
		ClientSocket.Send( bytes );
	}
	
	public void SendMessage( NetworkMessage message )
	{
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkable<NetworkMessage>( message );
		writer.Close();
		
		SendMessage( stream.ToArray() );
	}
	
	/// <summary>
	/// Called when data has been received from the client.
	/// </summary>
	/// <param name="stream">The data the client has sent.</param>
	protected virtual void OnDataReceived( MemoryStream stream )
	{
		var reader = new NetworkReader( stream );
		var message = NetworkMessage.DeserializeMessage( reader );
		reader.Close();
		
		NetworkServer.Instance.QueueIncoming( this, message );
	}

	/// <summary>
	/// Called when a message has been received from the client.
	/// </summary>
	/// <param name="message">The message the client has sent.</param>
	protected virtual void OnMessageReceived( string message )
	{
	}
}
#endif
