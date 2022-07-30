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
	public static readonly ConcurrentDictionary<Guid, RpcCallResponseMessage> RpcResponses = new();
	
	public static void Call( IEntity entity, string methodName, params INetworkable[] parameters )
	{
		Call( To.All, entity, methodName, parameters );
	}
	
	public static async Task<RpcCallResponseMessage> CallAsync( INetworkClient client, IEntity entity, string methodName,
		params INetworkable[] parameters )
	{
		var message = CreateRpc( true, entity, methodName, parameters );
		NetworkServer.Instance.QueueMessage( To.Single( client ), message );
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
		NetworkServer.Instance.QueueMessage( To.Single( client ), message );
		return await WaitForResponseAsync( message.CallGuid );
	}
	
	public static void Call( To to, IEntity entity, string methodName, params INetworkable[] parameters )
	{
		NetworkServer.Instance.QueueMessage( to, CreateRpc( false, entity, methodName, parameters ) );
	}
	
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
