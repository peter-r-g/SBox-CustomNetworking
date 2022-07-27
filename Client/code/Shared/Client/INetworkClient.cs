namespace CustomNetworking.Shared;

public interface INetworkClient
{
	long ClientId { get; }

	void SendMessage( byte[] bytes );
	void SendMessage( NetworkMessage message );
}
