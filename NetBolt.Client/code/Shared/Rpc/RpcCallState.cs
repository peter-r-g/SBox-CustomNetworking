namespace CustomNetworking.Shared.RemoteProcedureCalls;

/// <summary>
/// Represents a state the Rpc resulted in.
/// </summary>
public enum RpcCallState : byte
{
	Completed,
	Failed
}
