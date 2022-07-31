#if SERVER
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public partial class Rpc
{
	private static readonly ConcurrentDictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
	
	/// <summary>
	/// Executes an RPC relating to an entities instance.
	/// </summary>
	/// <param name="entity">The entity instance to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
		Call( To.All, entity, methodName, parameters );
	}
	
	/// <summary>
	/// Executes an asynchronous RPC relating to an entities instance.
	/// </summary>
	/// <param name="client">The client to send the RPC to.</param>
	/// <param name="entity">The entity instance to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkServer.Instance.QueueMessage( To.Single( client ), message );
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
		Call( To.All, type, methodName, parameters );
	}
	
	/// <summary>
	/// Executes an asynchronous RPC on a static method.
	/// </summary>
	/// <param name="client"></param>
	/// <param name="type">The type to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>A task that will complete once a <see cref="RpcCallResponseMessage"/> is received that contains the sent <see cref="RpcCallMessage"/>.<see cref="RpcCallMessage.CallGuid"/>.</returns>
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, Type type, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, type, methodName, parameters );
		NetworkServer.Instance.QueueMessage( To.Single( client ), message );
		return await WaitForResponseAsync( message.CallGuid );
	}
	
	/// <summary>
	/// Executes an RPC relating to an entities instance that is sent to specific clients.
	/// </summary>
	/// <param name="to">The clients to send the RPC to.</param>
	/// <param name="entity">The entity instance to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void Call( To to, IEntity entity, string methodName, params INetworkable[] parameters )
	{
		NetworkServer.Instance.QueueMessage( to, CreateRpc( false, entity, methodName, parameters ) );
	}
	
	/// <summary>
	/// Executes an RPC on a static method that is sent to specific clients.
	/// </summary>
	/// <param name="to">The clients to send the RPC to.</param>
	/// <param name="type">The type to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void Call( To to, Type type, string methodName, params INetworkable[] parameters )
	{
		NetworkServer.Instance.QueueMessage( to, CreateRpc( false, type, methodName, parameters ) );
	}
	
	internal static void HandleRpcCallMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not RpcCallMessage rpcCall )
			return;

		var type = TypeHelper.GetTypeByName( rpcCall.ClassName );
		if ( type is null )
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.ClassName}\" doesn't exist in the current assembly)." );

		var method = type.GetMethod( rpcCall.MethodName );
		if ( method is null )
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.MethodName}\" does not exist on \"{type}\")." );

		if ( method.GetCustomAttribute( typeof(Rpc.ServerAttribute) ) is null )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to invoke a non-RPC method)." );
		
		var instance = BaseGame.Current.GetNetworkedEntityById( rpcCall.EntityId );
		if ( instance is null && rpcCall.EntityId != -1 )
			throw new InvalidOperationException(
				"Failed to handle RPC call (Attempted to call RPC on a non-existant entity)." );

		var returnValue = method.Invoke( instance, rpcCall.Parameters );
		if ( rpcCall.CallGuid == Guid.Empty )
			return;

		if ( returnValue is not INetworkable && returnValue is not null )
		{
			var failedMessage = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Failed );
			NetworkServer.Instance.QueueMessage( To.Single( client ), failedMessage );
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.MethodName}\" returned a non-networkable value)." );
		}

		var response = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Completed,
			returnValue as INetworkable ?? null );
		NetworkServer.Instance.QueueMessage( To.Single( client ), response );
	}

	internal static void HandleRpcCallResponseMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not RpcCallResponseMessage rpcResponse )
			return;

		if ( !Rpc.RpcResponses.TryAdd( rpcResponse.CallGuid, rpcResponse ) )
			throw new InvalidOperationException( $"Failed to handle RPC call response (Failed to add \"{rpcResponse.CallGuid}\" to response dictionary)." );
	}
}
#endif
