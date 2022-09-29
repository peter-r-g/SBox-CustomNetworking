#if CLIENT && !MONITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CustomNetworking.Client;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.RemoteProcedureCalls;

public partial class Rpc
{
	/// <summary>
	/// The dictionary to hold RPC responses.
	/// </summary>
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

	/// <summary>
	/// Handles an incoming RPC from the server.
	/// </summary>
	/// <param name="message">The RPC call message.</param>
	/// <exception cref="InvalidOperationException">Thrown when handling the RPC call failed.</exception>
	internal static void HandleRpcCallMessage( NetworkMessage message )
	{
		if ( message is not RpcCallMessage rpcCall )
			return;

		var type = TypeHelper.GetTypeByName( rpcCall.ClassName );
		if ( type is null )
		{
			Logging.Error( $"Failed to handle RPC call (\"{rpcCall.ClassName}\" doesn't exist).", new InvalidOperationException() );
			return;
		}

		// TODO: Support instance methods https://github.com/Facepunch/sbox-issues/issues/2079
		var method = TypeLibrary.FindStaticMethods( rpcCall.MethodName ).FirstOrDefault();
		if ( method is null )
		{
			Logging.Error( $"Failed to handle RPC call (\"{rpcCall.MethodName}\" does not exist on \"{type}\").", new InvalidOperationException() );
			return;
		}
		
		if ( !method.Attributes.Any( attribute => attribute is ClientAttribute ) )
		{
			Logging.Error( "Failed to handle RPC call (Attempted to invoke a non-RPC method).", new InvalidOperationException() );
			return;
		}
		
		var entity = IEntity.All.GetEntityById( rpcCall.EntityId );
		if ( entity is null && rpcCall.EntityId != -1 )
		{
			Logging.Error( "Failed to handle RPC call (Attempted to call RPC on a non-existant entity).", new InvalidOperationException() );
			return;
		}

		var parameters = new List<object>();
		parameters.AddRange( rpcCall.Parameters );
		if ( entity is not null )
			parameters.Insert( 0, entity );
		
		if ( rpcCall.CallGuid == Guid.Empty )
		{
			method.Invoke( null, parameters.ToArray() );
			return;
		}

		var returnValue = method.InvokeWithReturn<object?>( null, parameters.ToArray() );
		if ( returnValue is not INetworkable && returnValue is not null )
		{
			var failedMessage = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Failed );
			NetworkManager.Instance?.SendToServer( failedMessage );
			Logging.Error( $"Failed to handle RPC call (\"{rpcCall.MethodName}\" returned a non-networkable value).", new InvalidOperationException() );
			return;
		}
		
		var response = new RpcCallResponseMessage( rpcCall.CallGuid, RpcCallState.Completed, returnValue as INetworkable ?? null );
		NetworkManager.Instance?.SendToServer( response );
	}
	
	/// <summary>
	/// Handles an incoming RPC call response.
	/// </summary>
	/// <param name="message">The RPC call response.</param>
	/// <exception cref="InvalidOperationException">Thrown when handling the RPC call response failed.</exception>
	internal static void HandleRpcCallResponseMessage( NetworkMessage message )
	{
		if ( message is not RpcCallResponseMessage rpcResponse )
			return;

		RpcResponses.Add( rpcResponse.CallGuid, rpcResponse );
	}
}
#endif
