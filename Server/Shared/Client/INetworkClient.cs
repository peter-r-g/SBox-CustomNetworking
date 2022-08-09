using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Shared;

/// <summary>
/// Contract to define something that is a client that can connect to a server.
/// </summary>
public interface INetworkClient
{
	/// <summary>
	/// The unique identifier of the client.
	/// </summary>
	long ClientId { get; }
	
	/// <summary>
	/// The player entity that the client is controlling.
	/// </summary>
	BasePlayer? Pawn { get; set; }

#if SERVER
	/// <summary>
	/// Sends an array of bytes to the client.
	/// </summary>
	/// <param name="bytes">The data to send to the client.</param>
	void SendMessage( byte[] bytes );
	/// <summary>
	/// Serializes a message and sends the data to the client.
	/// </summary>
	/// <param name="message">The message to send to the client.</param>
	void SendMessage( NetworkMessage message );
#endif
}
