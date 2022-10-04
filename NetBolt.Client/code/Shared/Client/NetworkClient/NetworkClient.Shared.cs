using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Shared;

/// <summary>
/// Base class for any non-bot clients connected to a server.
/// </summary>
public partial class NetworkClient : INetworkClient
{
	public event INetworkClient.PawnChangedEventHandler? PawnChanged;
	
	public long ClientId { get; }

	public IEntity? Pawn
	{
		get => _pawn;
		set
		{
			if ( value is not null && _pawn is not null )
				return;
			
			if ( value is not null && _pawn is not null && value.EntityId == _pawn.EntityId )
				return;
			
			var oldPawn = _pawn;
			_pawn = value;
			PawnChanged?.Invoke( this, oldPawn, _pawn );
		}
	}
	private IEntity? _pawn;

	public override string ToString()
	{
		return $"Client (ID: {ClientId})";
	}
}
