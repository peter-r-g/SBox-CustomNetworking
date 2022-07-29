using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Game;

public class GameInformationEntity : NetworkEntity
{
	public NetworkedList<NetworkedInt> TestItems
	{
		get => _testItems;
		set
		{
			_testItems = value;
#if SERVER
			TriggerNetworkingChange( nameof(TestItems) );
#endif
		}
	}
	private NetworkedList<NetworkedInt> _testItems = new();

	public GameInformationEntity( int entityId ) : base( entityId )
	{
	}
}
