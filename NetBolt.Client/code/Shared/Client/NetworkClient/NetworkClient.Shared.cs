using NetBolt.Shared.Entities;
#if SERVER
using NetBolt.WebSocket;
#endif

namespace NetBolt.Shared;

/// <summary>
/// Base class for any non-bot clients connected to a server.
/// </summary>
#if SERVER
public partial class NetworkClient : WebSocketClient, INetworkClient
#endif
#if CLIENT
public partial class NetworkClient : INetworkClient
#endif
{
	public event INetworkClient.PawnChangedEventHandler? PawnChanged;
	
	public long ClientId { get; private set; }

	public bool IsBot => false;

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
