using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public static class NetworkEntityRpcExtension
{
	public static void CallRpc( this IEntity entity, string methodName, params INetworkable[] parameters )
	{
		Rpc.Call( entity, methodName, parameters );
	}

#if CLIENT
	public static async Task<RpcCallResponseMessage> CallRpcAsync( this IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		return await Rpc.CallAsync( entity, methodName, parameters );
	}
#endif

#if SERVER
	public static void CallRpc( this IEntity entity, To to, string methodName, params INetworkable[] parameters )
	{
		Rpc.Call( to, entity, methodName, parameters );
	}
	
	public static async Task<RpcCallResponseMessage> CallRpcAsync( this IEntity entity, INetworkClient client,
		string methodName, params INetworkable[] parameters )
	{
		return await Rpc.CallAsync( client, entity, methodName, parameters );
	}
#endif
}
