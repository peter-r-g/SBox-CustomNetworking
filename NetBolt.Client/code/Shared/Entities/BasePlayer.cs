#if CLIENT
using NetBolt.Client;
using Sandbox;
#endif

namespace NetBolt.Shared.Entities;

/// <summary>
/// A test class for player input and basic entity networking.
/// </summary>
public class BasePlayer : NetworkEntity
{
#if CLIENT
	/// <summary>
	/// The Sbox entity of the player.
	/// </summary>
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
		{
			_player.Position = Position;
			_player.Rotation = Rotation;
			return;
		}

		if ( _player.Position.Distance( Position ) >= 0.001 )
			Position = _player.Position;

		if ( _player.EyeRotation.Distance( Rotation ) >= 0.1 )
			Rotation = _player.EyeRotation;
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
#endif
}
