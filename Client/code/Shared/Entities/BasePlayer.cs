#if CLIENT
using System;
using CustomNetworking.Shared.Networkables.Builtin;
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

		if ( _player.Position.Distance( Position ) >= 0.001 )
			Position = _player.Position;

		if ( _player.Rotation.Distance( Rotation ) >= 1 )
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

	protected override void OnPositionChanged( object? sender, EventArgs args )
	{
		base.OnPositionChanged( sender, args );

		_player.Position = (NetworkedVector3)sender;
	}

	protected override void OnRotationChanged( object? sender, EventArgs args )
	{
		base.OnRotationChanged( sender, args );

		_player.Rotation = (NetworkedQuaternion)sender;
	}
#endif
}
