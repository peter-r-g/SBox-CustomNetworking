namespace CustomNetworking.Shared;

public interface INetworkClient
{
	long ClientId { get; }

#if SERVER
	void SendMessage( byte[] bytes );
	void SendMessage( NetworkMessage message );
#endif
}
