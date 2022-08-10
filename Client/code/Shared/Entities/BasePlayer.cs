#if CLIENT
using CustomNetworking.Client;
using Sandbox;
#endif

namespace CustomNetworking.Shared.Entities;

public class BasePlayer  : NetworkEntity
{
#if CLIENT
	private readonly TestPlayer _player;
#endif
	
	public BasePlayer( int entityId ) : base( entityId )
	{
#if CLIENT
		_player = new TestPlayer();
#endif
	}

#if CLIENT
	protected override void UpdateClient()
	{
		base.UpdateClient();

		if ( Local.Client.Pawn != _player )
			return;

		if ( !Position.Equals( _player.Position ) )
			Position = _player.Position;

		if ( !Rotation.Equals( _player.Rotation ) )
			Rotation = _player.Rotation;
	}
#endif

	public override void Delete()
	{
		base.Delete();
		
#if CLIENT
		_player.Delete();
		
		if ( Local.Client is not null && Local.Client.Pawn == _player )
			Local.Client.Pawn = null;
#endif
	}

	protected override void OnOwnerChanged( INetworkClient oldOwner, INetworkClient newOwner )
	{
		base.OnOwnerChanged( oldOwner, newOwner );

#if CLIENT
		if ( oldOwner == INetworkClient.Local )
			Local.Client.Pawn = null;

		if ( newOwner == INetworkClient.Local )
			Local.Client.Pawn = _player;
#endif
	}
}
