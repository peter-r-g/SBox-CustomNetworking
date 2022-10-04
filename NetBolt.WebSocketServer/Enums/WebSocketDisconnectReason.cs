namespace NetBolt.WebSocket.Enums;

/// <summary>
/// Represents a reason for a <see cref="IWebSocketClient"/> to disconnect.
/// </summary>
public enum WebSocketDisconnectReason
{
	/// <summary>
	/// No reason.
	/// </summary>
	None,
	/// <summary>
	/// An error occurred in the server-side.
	/// </summary>
	Error,
	/// <summary>
	/// Disconnect was requested by the client.
	/// </summary>
	Requested,
	/// <summary>
	/// The server is shutting down.
	/// </summary>
	ServerShutdown,
	/// <summary>
	/// Failed to receive any messages from the client for X amount of time.
	/// </summary>
	Timeout
}
