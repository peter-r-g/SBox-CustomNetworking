#if CLIENT
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomNetworking.Client;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public partial class Rpc
{
	public static readonly Dictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
	
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.Instance?.SendToServer( CreateRpc( false, entity, methodName, parameters ) );
	}

	public static async Task<RpcCallResponseMessage> CallAsync( IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkManager.Instance?.SendToServer( message );
		return await WaitForResponseAsync( message.CallGuid );
	}

	public static void Call( Type type, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.Instance?.SendToServer( CreateRpc( false, type, methodName, parameters ) );
	}

	public static async Task<RpcCallResponseMessage> CallAsync( Type type, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, type, methodName, parameters );
		NetworkManager.Instance?.SendToServer( message );
		return await WaitForResponseAsync( message.CallGuid );
	}
}
#endif
