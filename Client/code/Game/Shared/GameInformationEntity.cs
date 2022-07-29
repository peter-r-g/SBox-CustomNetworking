using CustomNetworking.Server;
using CustomNetworking.Shared;
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

#if CLIENT
	protected override void UpdateClient()
	{
		base.UpdateClient();

		ServerRpc( 1 );
	}
#endif
	
#if SERVER
	protected override void UpdateServer()
	{
		base.UpdateServer();
		
		ClientRpc( this, 1 );
	}
#endif

	public static void ClientRpc( GameInformationEntity instance, NetworkedInt i )
	{
#if SERVER
		Rpc.Call( instance, nameof(ClientRpc), i );
#endif
#if CLIENT
		Log.Info( "Client RPC executed!" );
#endif
	}

	public void ServerRpc( NetworkedInt i )
	{
#if CLIENT
		Rpc.Call( this, nameof(ServerRpc), i );
#endif
#if SERVER
		Program.Logger.Enqueue( "Server RPC executed!" );
#endif
	}
}
