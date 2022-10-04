#if CLIENT
namespace NetBolt.Shared;

public partial class NetworkClient
{
	internal NetworkClient( long clientId )
	{
		ClientId = clientId;
	}
}
#endif
