#if SERVER
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using NetBolt.Server;
using NetBolt.Shared.Entities;
using NetBolt.Shared.Messages;
using NetBolt.Shared.Networkables;
using NetBolt.Shared.Utility;
using NetBolt.WebSocket;

namespace NetBolt.Shared.RemoteProcedureCalls;

public partial class Rpc
{
	/// <summary>
	/// The dictionary to hold RPC responses.
	/// </summary>
	private static readonly ConcurrentDictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
	
	/// <summary>
	/// Executes an RPC relating to an entities instance.
	/// </summary>
	/// <param name="entity">The entity instance to call the RPC on.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
		Call( To.All( NetworkServer.Instance ), entity, methodName, parameters );
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
		NetworkServer.Instance.QueueSend( To.Single( client ), message );
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
		Call( To.All( NetworkServer.Instance ), type, methodName, parameters );
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
		NetworkServer.Instance.QueueSend( To.Single( client ), message );
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
		NetworkServer.Instance.QueueSend( to, CreateRpc( false, entity, methodName, parameters ) );
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
		NetworkServer.Instance.QueueSend( to, CreateRpc( false, type, methodName, parameters ) );
	}
	
	/// <summary>
	/// Handles an incoming RPC from a client.
	/// </summary>
	/// <param name="client">The client that sent the RPC.</param>
	/// <param name="message">The RPC call message.</param>
	/// <exception cref="InvalidOperationException">Thrown when handling the RPC call failed.</exception>
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

		if ( method.GetCustomAttribute( typeof(ServerAttribute) ) is null )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to invoke a non-RPC method)." );
		
		var entity = BaseGame.Current.GetNetworkedEntityById( rpcCall.EntityId );
		if ( entity is null && rpcCall.EntityId != -1 )
			throw new InvalidOperationException(
				"Failed to handle RPC call (Attempted to call RPC on a non-existant entity)." );

		var returnValue = method.Invoke( entity, rpcCall.Parameters );
		if ( rpcCall.CallGuid == Guid.Empty )
			return;

		if ( returnValue is not INetworkable && returnValue is not null )
		{
			var failedMessage = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Failed );
			NetworkServer.Instance.QueueSend( To.Single( client ), failedMessage );
			throw new InvalidOperationException(
				$"Failed to handle RPC call (\"{rpcCall.MethodName}\" returned a non-networkable value)." );
		}

		var response = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Completed,
			returnValue as INetworkable ?? null );
		NetworkServer.Instance.QueueSend( To.Single( client ), response );
	}

	/// <summary>
	/// Handles an incoming RPC call response.
	/// </summary>
	/// <param name="client">The client that sent the response.</param>
	/// <param name="message">The RPC call response.</param>
	/// <exception cref="InvalidOperationException">Thrown when handling the RPC call response failed.</exception>
	internal static void HandleRpcCallResponseMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not RpcCallResponseMessage rpcResponse )
			return;

		if ( !RpcResponses.TryAdd( rpcResponse.CallGuid, rpcResponse ) )
			throw new InvalidOperationException( $"Failed to handle RPC call response (Failed to add \"{rpcResponse.CallGuid}\" to response dictionary)." );
	}
}
#endif
