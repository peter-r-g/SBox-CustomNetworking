using System;

namespace NetBolt.WebSocket.Exceptions;

/// <summary>
/// Thrown when trying to use logic that requires a <see cref="IWebSocketServer"/> to be running and is not.
/// </summary>
public sealed class ServerNotRunningException : Exception
{
	public ServerNotRunningException() : base( "Server is not running" )
	{
	}
}
