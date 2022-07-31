namespace CustomNetworking.Shared;

/// <summary>
/// Base class for any non-bot clients connected to a server.
/// </summary>
public partial class NetworkClient : INetworkClient
{
	public long ClientId { get; }
}
