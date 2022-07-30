#if SERVER
using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Server.Shared.Rpc;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Game;

public partial class GameInformationEntity
{
	private Task _getClientValueTask = Task.CompletedTask;
	
	protected override void UpdateServer()
	{
		base.UpdateServer();
		
		ClientRpc( 1 );
		if ( _getClientValueTask.IsCompleted )
			_getClientValueTask = GetClientValue();
	}
	
	public void ClientRpc( NetworkedInt i )
	{
		Rpc.Call( this, nameof(ClientRpc), i );
	}

	public async Task GetClientValue()
	{
		if ( NetworkManager.Clients.IsEmpty )
			return;
		
		var response = await Rpc.CallAsync( NetworkManager.Clients[0], this, nameof(GetClientValue) );
		if ( response.State == RpcCallState.Failed )
			return;
		
		Program.Logger.Enqueue( $"Got {response.ReturnValue} from client RPC" );
	}

	public void ServerRpc( NetworkedInt i )
	{
		Program.Logger.Enqueue( "Server RPC executed!" );
	}

	public NetworkedInt GetServerValue()
	{
		return 2;
	}
}
#endif