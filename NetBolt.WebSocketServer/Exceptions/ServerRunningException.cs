using System;

namespace NetBolt.WebSocket.Exceptions;

/// <summary>
/// Thrown when trying to use logic that requires a <see cref="IWebSocketServer"/> to not be running and is.
/// </summary>
public sealed class ServerRunningException : Exception
{
	public ServerRunningException() : base( "Server is running" )
	{
	}
}
