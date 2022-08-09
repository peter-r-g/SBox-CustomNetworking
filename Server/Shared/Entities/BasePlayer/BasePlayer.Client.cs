#if CLIENT
using CustomNetworking.Client;
using Sandbox;

namespace CustomNetworking.Shared.Entities;

public partial class BasePlayer
{
	protected TestPlayer PlayerPawn;

	public BasePlayer()
	{
		PlayerPawn = new TestPlayer();
		Local.Client.Pawn = PlayerPawn;
	}
}
#endif
