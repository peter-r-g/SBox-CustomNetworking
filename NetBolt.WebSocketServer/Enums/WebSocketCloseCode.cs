namespace NetBolt.WebSocket.Enums;

/// <summary>
/// Represents a valid web socket close code.
/// <remarks>https://www.rfc-editor.org/rfc/rfc6455#section-7.4.1</remarks>
/// </summary>
public enum WebSocketCloseCode
{
	Normal = 1000,
	Shutdown = 1001,
	ProtocolError = 1002,
	UnacceptableData = 1003,
	Reserved = 1004,
	ReservedMissing = 1005,
	ReservedAbnormal = 1006,
	InconsistentData = 1007,
	PolicyViolation = 1008,
	MessageTooLarge = 1009,
	ExtensionMissing = 1010,
	UnexpectedError = 1011,
	TlsFailure = 1015
}
