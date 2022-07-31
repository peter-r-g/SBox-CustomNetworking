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
	private static readonly Dictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
	
	/// <summary>
	/// Executes an RPC relating to an entities instance.
	/// </summary>
	/// <param name="entity">The entity instance to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.Instance?.SendToServer( CreateRpc( false, entity, methodName, parameters ) );
	}

	/// <summary>
	/// Executes an asynchronous RPC relating to an entities instance.
	/// </summary>
	/// <param name="client">The client to send the RPC to.</param>
	/// <param name="entity">The entity instance to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallAsync( IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkManager.Instance?.SendToServer( message );
		return await WaitForResponseAsync( message.CallGuid );
	}

	/// <summary>
	/// Executes an RPC on a static method.
	/// </summary>
	/// <param name="type">The type to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void Call( Type type, string methodName, params INetworkable[] parameters )
	{
		NetworkManager.Instance?.SendToServer( CreateRpc( false, type, methodName, parameters ) );
	}

	/// <summary>
	/// Executes an asynchronous RPC on a static method.
	/// </summary>
	/// <param name="client"></param>
	/// <param name="type">The type to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallAsync( Type type, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, type, methodName, parameters );
		NetworkManager.Instance?.SendToServer( message );
		return await WaitForResponseAsync( message.CallGuid );
	}
}
#endif
