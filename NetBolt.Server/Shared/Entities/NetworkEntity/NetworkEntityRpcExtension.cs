#if !MONITOR
using System.Threading.Tasks;
#if SERVER
using NetBolt.Server;
#endif
using NetBolt.Shared.Entities;
using NetBolt.Shared.Messages;
using NetBolt.Shared.Networkables;

namespace NetBolt.Shared.RemoteProcedureCalls;

/// <summary>
/// Extension of helper entity methods to call RPCs on themselves.
/// </summary>
public static class NetworkEntityRpcExtension
{
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.Call( IEntity, string, INetworkable[] )"/>.
	/// </summary>
	/// <param name="entity">The entity to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void CallRpc( this IEntity entity, string methodName, params INetworkable[] parameters )
	{
		Rpc.Call( entity, methodName, parameters );
	}
	
#if CLIENT
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.CallAsync( IEntity, string, INetworkable[] )"/>.
	/// </summary>
	/// <param name="entity">The entity to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallRpcAsync( this IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		return await Rpc.CallAsync( entity, methodName, parameters );
	}
#endif

#if SERVER
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.Call( To, IEntity, string, INetworkable[] )"/>.
	/// </summary>
	/// <param name="entity">The entity to call the RPC on.</param>
	/// <param name="to">The clients to execute the RPC.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void CallRpc( this IEntity entity, To to, string methodName, params INetworkable[] parameters )
	{
		Rpc.Call( to, entity, methodName, parameters );
	}
	
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.CallAsync( INetworkClient, IEntity, string, INetworkable[] )"/>.
	/// </summary>
	/// <param name="entity">The entity to call the RPC on.</param>
	/// <param name="client">The client to execute the RPC.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallRpcAsync( this IEntity entity, INetworkClient client,
		string methodName, params INetworkable[] parameters )
	{
		return await Rpc.CallAsync( client, entity, methodName, parameters );
	}
#endif
}
#endif
