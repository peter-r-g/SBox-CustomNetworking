using System;
#if SERVER
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Threading.Tasks;
#if CLIENT
using CustomNetworking.Client;
#endif
#if SERVER
using CustomNetworking.Server;
#endif
using CustomNetworking.Server.Shared.Messages;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public static class Rpc
{
#if SERVER
	public static readonly ConcurrentDictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
#endif
#if CLIENT
	public static readonly Dictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
#endif
	
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
#if SERVER
		Call( To.All, entity, methodName, parameters );
#endif
#if CLIENT
		NetworkManager.Instance?.SendToServer( CreateRpc( false, entity, methodName, parameters ) );
#endif
	}

#if SERVER
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, IEntity entity, string methodName,
		params INetworkable[] parameters )
#endif
#if CLIENT
	public static async Task<RpcCallResponseMessage> CallAsync( IEntity entity, string methodName,
		params INetworkable[] parameters )
#endif
	{
#if SERVER
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkManager.QueueMessage( To.Single( client ), message );
		return await WaitForResponseAsync( message.CallGuid );
#endif
#if CLIENT
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkManager.Instance?.SendToServer( message );
		return await WaitForResponseAsync( message.CallGuid );
#endif
	}

	public static void Call( Type type, string methodName, params INetworkable[] parameters )
	{
#if SERVER
		Call( To.All, type, methodName, parameters );
#endif
#if CLIENT
		NetworkManager.Instance?.SendToServer( CreateRpc( false, type, methodName, parameters ) );
#endif
	}

#if SERVER
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, Type type, string methodName,
		params INetworkable[] parameters )
#endif
#if CLIENT
	public static async Task<RpcCallResponseMessage> CallAsync( Type type, string methodName,
		params INetworkable[] parameters )
#endif
	{
#if SERVER
		var message = CreateRpc( true, type, methodName, parameters );
		NetworkManager.QueueMessage( To.Single( client ), message );
		return await WaitForResponseAsync( message.CallGuid );
#endif
#if CLIENT
		var message = CreateRpc( true, type, methodName, parameters );
		NetworkManager.Instance?.SendToServer( message );
		return await WaitForResponseAsync( message.CallGuid );
#endif
	}
	
#if SERVER
	public static void Call( To to, IEntity entity, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.QueueMessage( to, CreateRpc( false, entity, methodName, parameters ) );
	}
	
	public static void Call( To to, Type type, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.QueueMessage( to, CreateRpc( false, type, methodName, parameters ) );
	}
#endif

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
