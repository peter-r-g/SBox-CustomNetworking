using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomNetworking.Server.Shared.Messages;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public static partial class Rpc
{
	private static RpcCallMessage CreateRpc( bool respondable, IEntity entity, string methodName, INetworkable[] parameters )
	{
		return new RpcCallMessage( respondable, entity.GetType(), entity, methodName, parameters );
	}

	private static RpcCallMessage CreateRpc( bool respondable, Type type, string methodName, INetworkable[] parameters )
	{
		return new RpcCallMessage( respondable, type, null, methodName, parameters );
	}

	private static async Task<RpcCallResponseMessage> WaitForResponseAsync( Guid callGuid )
	{
		// TODO: Surely there's a better way to do this right?
		// TODO: This does not account for disconnects or the environment shutting down.
		while ( !RpcResponses.ContainsKey( callGuid  ) )
			await Task.Delay( 1 );
		
		RpcResponses.Remove( callGuid, out var response );
		if ( response is null )
			throw new InvalidOperationException( $"Failed to return RPC response (\"{callGuid}\" became invalid unexpectedly)." );
		
		return response;
	}
}
