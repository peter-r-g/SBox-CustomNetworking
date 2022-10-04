using System;
using NetBolt.WebSocket.Enums;

namespace NetBolt.WebSocket.Extensions;

/// <summary>
/// Extension class for <see cref="WebSocketDisconnectReason"/>.
/// </summary>
public static class WebSocketDisconnectReasonExtensions
{
	/// <summary>
	/// Converts a <see cref="WebSocketDisconnectReason"/> to a valid <see cref="WebSocketCloseCode"/>.
	/// </summary>
	/// <param name="reason">The reason to convert.</param>
	/// <returns>The converted close code.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the <see cref="reason"/> passed is invalid.</exception>
	public static WebSocketCloseCode GetCloseCode( this WebSocketDisconnectReason reason )
	{
		return reason switch
		{
			WebSocketDisconnectReason.None => WebSocketCloseCode.Normal,
			WebSocketDisconnectReason.Error => WebSocketCloseCode.UnexpectedError,
			WebSocketDisconnectReason.Requested => WebSocketCloseCode.Normal,
			WebSocketDisconnectReason.ServerShutdown => WebSocketCloseCode.Shutdown,
			WebSocketDisconnectReason.Timeout => WebSocketCloseCode.ProtocolError,
			_ => throw new ArgumentOutOfRangeException( nameof(reason), reason, null )
		};
	}
}
