#if CLIENT
namespace CustomNetworking.Shared;

public partial class NetworkClient
{
	internal NetworkClient( long clientId )
	{
		ClientId = clientId;
	}
}
#endif
