#if SERVER
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public partial class Rpc
{
	public static readonly ConcurrentDictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
	
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
		Call( To.All, entity, methodName, parameters );
	}
	
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkManager.QueueMessage( To.Single( client ), message );
		return await WaitForResponseAsync( message.CallGuid );
	}

	public static void Call( Type type, string methodName, params INetworkable[] parameters )
	{
		Call( To.All, type, methodName, parameters );
	}
	
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, Type type, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, type, methodName, parameters );
		NetworkManager.QueueMessage( To.Single( client ), message );
		return await WaitForResponseAsync( message.CallGuid );
	}
	
	public static void Call( To to, IEntity entity, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.QueueMessage( to, CreateRpc( false, entity, methodName, parameters ) );
	}
	
	public static void Call( To to, Type type, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.QueueMessage( to, CreateRpc( false, type, methodName, parameters ) );
	}
}
#endif
