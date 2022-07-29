#if CLIENT
namespace CustomNetworking.Shared;

public partial class NetworkClient
{
	public NetworkClient( long clientId )
	{
		ClientId = clientId;
	}
}
#endif
