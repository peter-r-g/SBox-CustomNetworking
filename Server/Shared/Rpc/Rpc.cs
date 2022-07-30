using System;
#if SERVER
using System.Collections.Concurrent;
#endif
#if CLIENT
using CustomNetworking.Client;
#endif
#if SERVER
using CustomNetworking.Server;
#endif
using CustomNetworking.Server.Shared.Messages;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public static class Rpc
{
	
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
#if SERVER
		Call( To.All, entity, methodName, parameters );
#endif
#if CLIENT
		NetworkManager.Instance?.SendToServer( CreateRpc( false, entity, methodName, parameters ) );
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

