using NetBolt.WebSocket.Exceptions;

namespace NetBolt.WebSocket.Utility;

/// <summary>
/// Utility class for throwing errors where necessary.
/// </summary>
internal static class WebSocketThrowHelper
{
	/// <summary>
	/// Checks and throws if a <see cref="IWebSocketClient"/> is disconnected.
	/// </summary>
	/// <param name="client">The <see cref="IWebSocketClient"/> to check if it is disconnected.</param>
	/// <exception cref="ClientDisconnectedException">Thrown if the <see cref="client"/> is disconnected.</exception>
	internal static void ThrowIfDisconnected( this IWebSocketClient client )
	{
		if ( !client.Connected )
			throw new ClientDisconnectedException( client );
	}

	/// <summary>
	/// Checks and throws if a <see cref="IWebSocketServer"/> is running.
	/// </summary>
	/// <param name="server">The <see cref="IWebSocketServer"/> to check if it is running.</param>
	/// <exception cref="ServerRunningException">Thrown if the <see cref="server"/> is running.</exception>
	internal static void ThrowIfRunning( this IWebSocketServer server )
	{
		if ( server.Running )
			throw new ServerRunningException();
	}

	/// <summary>
	/// Checks and throws if a <see cref="IWebSocketServer"/> is not running.
	/// </summary>
	/// <param name="server">The <see cref="IWebSocketServer"/> to check if it is not running.</param>
	/// <exception cref="ServerNotRunningException">Thrown if the <see cref="server"/> is not running.</exception>
	internal static void ThrowIfNotRunning( this IWebSocketServer server )
	{
		if ( !server.Running )
			throw new ServerNotRunningException();
	}
}
