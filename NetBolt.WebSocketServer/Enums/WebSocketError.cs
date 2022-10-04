using System.Net.Sockets;

namespace NetBolt.WebSocket.Enums;

/// <summary>
/// Represents the type of error the server experienced in handling the <see cref="IWebSocketClient"/>.
/// </summary>
public enum WebSocketError
{
	/// <summary>
	/// An error in handling incoming data.
	/// </summary>
	HandlingException,
	/// <summary>
	/// Received an unfinished message from the client.
	/// </summary>
	MessageUnfinished,
	/// <summary>
	/// Received a message that had no mask.
	/// </summary>
	MissingMask,
	/// <summary>
	/// Tried to use a disposed <see cref="NetworkStream"/>. Most likely due to unexpected disconnect.
	/// </summary>
	StreamDisposed,
	/// <summary>
	/// Failed to upgrade the client to the web socket protocol.
	/// </summary>
	UpgradeFail
}
