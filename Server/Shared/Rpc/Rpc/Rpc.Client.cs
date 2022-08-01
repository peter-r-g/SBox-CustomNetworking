#if CLIENT && !MONITOR
using System;
using System.Collections.Generic;
using System.Linq;
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

	internal static void HandleRpcCallMessage( NetworkMessage message )
	{
		if ( message is not RpcCallMessage rpcCall )
			return;

		var type = TypeLibrary.GetTypeByName( rpcCall.ClassName );
		if ( type is null )
			throw new InvalidOperationException( $"Failed to handle RPC call (\"{rpcCall.ClassName}\" doesn't exist in the current assembly)." );

		// TODO: Support instance methods https://github.com/Facepunch/sbox-issues/issues/2079
		var method = TypeLibrary.FindStaticMethods( rpcCall.MethodName ).FirstOrDefault();
		if ( method is null )
			throw new InvalidOperationException( $"Failed to handle RPC call (\"{rpcCall.MethodName}\" does not exist on \"{type}\")." );
		
		if ( !method.Attributes.Any( attribute => attribute is Rpc.ClientAttribute ) )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to invoke a non-RPC method)." );
		
		var instance = MyGame.Current.EntityManager?.GetEntityById( rpcCall.EntityId );
		if ( instance is null && rpcCall.EntityId != -1 )
			throw new InvalidOperationException( "Failed to handle RPC call (Attempted to call RPC on a non-existant entity)." );

		var parameters = new List<object>();
		parameters.AddRange( rpcCall.Parameters );
		if ( instance is not null )
			parameters.Insert( 0, instance );
		
		if ( rpcCall.CallGuid == Guid.Empty )
		{
			method.Invoke( null, parameters.ToArray() );
			return;
		}

		var returnValue = method.InvokeWithReturn<object?>( null, parameters.ToArray() );
		if ( returnValue is not INetworkable && returnValue is not null )
		{
			var failedMessage = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Failed );
			_ = NetworkManager.Instance?.SendToServer( failedMessage );
			throw new InvalidOperationException( $"Failed to handle RPC call (\"{rpcCall.MethodName}\" returned a non-networkable value)." );
		}
		
		var response = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Completed, returnValue as INetworkable ?? null );
		_ = NetworkManager.Instance?.SendToServer( response );
	}
	
	internal static void HandleRpcCallResponseMessage( NetworkMessage message )
	{
		if ( message is not RpcCallResponseMessage rpcResponse )
			return;

		Rpc.RpcResponses.Add( rpcResponse.CallGuid, rpcResponse );
	}
}
#endif
