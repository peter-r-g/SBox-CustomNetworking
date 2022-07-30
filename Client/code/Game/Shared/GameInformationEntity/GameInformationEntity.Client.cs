#if CLIENT
using System.Threading.Tasks;
using CustomNetworking.Server.Shared.Rpc;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Game;

public partial class GameInformationEntity
{
	// TODO: Can't use Task.CompletedTask like in GameInformationEntity.Server.cs as it is not whitelisted :|
	private Task? _getServerValueTask;

	protected override void UpdateClient()
	{
		base.UpdateClient();

		ServerRpc( 1 );
		if ( _getServerValueTask is null || _getServerValueTask.IsCompleted )
			_getServerValueTask = GetServerValue();
	}

	public static void ClientRpc( GameInformationEntity instance, NetworkedInt i )
	{
		Log.Info( "Client RPC executed!" );
	}

	public static NetworkedInt GetClientValue( GameInformationEntity instance )
	{
		return 1;
	}

	public void ServerRpc( NetworkedInt i )
	{
		Rpc.Call( this, nameof(ServerRpc), i );
	}

	public async Task GetServerValue()
	{
		var response = await Rpc.CallAsync( this, nameof(GetServerValue) );
		if ( response.State == RpcCallState.Failed )
			return;
		
		Log.Info( $"Got {response.ReturnValue} from server RPC" );
	}
}
#endif
