#if SERVER
using System;
using System.Linq;
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
		this.CallRpc( nameof(ClientRpc), i );
	}

	public async Task GetClientValue()
	{
		if ( NetworkManager.Clients.IsEmpty )
			return;

		var response = await this.CallRpcAsync( NetworkManager.Clients.First().Value, nameof(GetClientValue) );
		if ( response.State == RpcCallState.Failed )
			return;
		
		Program.Logger.Enqueue( $"Got {response.ReturnValue} from client RPC" );
	}

	[Rpc.Server]
	public void ServerRpc( NetworkedInt i )
	{
		Program.Logger.Enqueue( "Server RPC executed!" );
	}

	[Rpc.Server]
	public NetworkedInt GetServerValue()
	{
		return 2;
	}
}
#endif
