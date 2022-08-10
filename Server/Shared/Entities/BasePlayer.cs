using CustomNetworking.Shared.Networkables.Builtin;
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

#if CLIENT
	public override void Delete()
	{
		base.Delete();
		
		_player.Delete();
		
		if ( Local.Client is not null && Local.Client.Pawn == _player )
			Local.Client.Pawn = null;
	}

	protected override void OnOwnerChanged( INetworkClient? oldOwner, INetworkClient? newOwner )
	{
		base.OnOwnerChanged( oldOwner, newOwner );
		
		if ( oldOwner == INetworkClient.Local )
			Local.Client.Pawn = null;

		if ( newOwner == INetworkClient.Local )
			Local.Client.Pawn = _player;
	}

	protected override void OnPositionChanged( NetworkedVector3 oldPosition, NetworkedVector3 newPosition )
	{
		base.OnPositionChanged( oldPosition, newPosition );

		_player.Position = newPosition;
	}

	protected override void OnRotationChanged( NetworkedQuaternion oldRotation, NetworkedQuaternion newRotation )
	{
		base.OnRotationChanged( oldRotation, newRotation );

		_player.Rotation = newRotation;
	}
#endif
}
