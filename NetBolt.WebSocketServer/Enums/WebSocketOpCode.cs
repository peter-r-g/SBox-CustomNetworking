namespace NetBolt.WebSocket.Enums;

/// <summary>
/// Represents a web socket message op code.
/// <remarks>https://www.rfc-editor.org/rfc/rfc6455#section-7.4.1</remarks>
/// </summary>
public enum WebSocketOpCode
{
	Continuation = 0,
	Text = 1,
	Binary = 2,
	NonControl0 = 3,
	NonControl1 = 4,
	NonControl2 = 5,
	NonControl3 = 6,
	NonControl4 = 7,
	Close = 8,
	Ping = 9,
	Pong = 10,
	Control0 = 11,
	Control1 = 12,
	Control2 = 13,
	Control3 = 14,
	Control4 = 15
}
