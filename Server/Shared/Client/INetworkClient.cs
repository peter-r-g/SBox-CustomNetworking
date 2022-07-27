using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Shared;

public interface INetworkClient
{
	long ClientId { get; }
	NetworkEntity? Pawn { get; }

	void SendMessage( byte[] bytes );
	void SendMessage( NetworkMessage message );
}
