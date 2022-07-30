using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Game;

public partial class GameInformationEntity : NetworkEntity
{
	public NetworkedList<NetworkedInt> TestItems
	{
		get => _testItems;
		set
		{
			_testItems.Changed -= OnTestItemsChanged;
			_testItems = value;
			value.Changed += OnTestItemsChanged;
			OnTestItemsChanged( value );
		}
	}
	private NetworkedList<NetworkedInt> _testItems = new();

	public GameInformationEntity( int entityId ) : base( entityId )
	{
	}
	
	protected virtual void OnTestItemsChanged( INetworkable networkable )
	{
#if SERVER
		TriggerNetworkingChange( nameof(Position) );
#endif
	}
}
