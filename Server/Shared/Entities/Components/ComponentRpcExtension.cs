#if !MONITOR
using System.Threading.Tasks;
#if SERVER
using CustomNetworking.Server;
#endif
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.RemoteProcedureCalls;

/// <summary>
/// Extension of helper component methods to call RPCs on themselves.
/// </summary>
public static class ComponentRpcExtension
{
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.Call( IEntity, string, string?, INetworkable[] )"/>.
	/// </summary>
	/// <param name="component">The entity component to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void CallRpc( this BaseComponent component, string methodName, params INetworkable[] parameters )
	{
		Rpc.Call( component.Entity, methodName, component.GetType().Name, parameters );
	}
	
#if CLIENT
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.CallAsync( IEntity, string, string?, INetworkable[] )"/>.
	/// </summary>
	/// <param name="component">The entity component to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallRpcAsync( this BaseComponent component, string methodName,
		params INetworkable[] parameters )
	{
		return await Rpc.CallAsync( component.Entity, methodName, component.GetType().Name, parameters );
	}
#endif

#if SERVER
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.Call( To, IEntity, string, string?, INetworkable[] )"/>.
	/// </summary>
	/// <param name="component">The entity component to call the RPC on.</param>
	/// <param name="to">The clients to execute the RPC.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void CallRpc( this BaseComponent component, To to, string methodName, params INetworkable[] parameters )
	{
		Rpc.Call( to, component.Entity, methodName, component.GetType().Name, parameters );
	}
	
	/// <summary>
	/// Wrapper for <see cref="Rpc"/>.<see cref="Rpc.CallAsync( INetworkClient, IEntity, string, string?, INetworkable[] )"/>.
	/// </summary>
	/// <param name="component">The entity component to call the RPC on.</param>
	/// <param name="client">The client to execute the RPC.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallRpcAsync( this BaseComponent component, INetworkClient client,
		string methodName, params INetworkable[] parameters )
	{
		return await Rpc.CallAsync( client, component.Entity, methodName, component.GetType().Name, parameters );
	}
#endif
}
#endif
