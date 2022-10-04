#if !MONITOR
using System;
#if SERVER
using System.Collections.Generic;
#endif
using System.Threading.Tasks;
using NetBolt.Shared.Entities;
using NetBolt.Shared.Messages;
using NetBolt.Shared.Networkables;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.RemoteProcedureCalls;

/// <summary>
/// A collection of methods to execute Remote Procedure Calls (RPCs).
/// </summary>
public static partial class Rpc
{
	/// <summary>
	/// Creates an RPC call message for an entity.
	/// </summary>
	/// <param name="respondable">Whether or not the RPC is expecting a response.</param>
	/// <param name="entity">The entity that is the target of the RPC.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>The built RPC message.</returns>
	private static RpcCallMessage CreateRpc( bool respondable, IEntity entity, string methodName,
		INetworkable[] parameters )
	{
		return new RpcCallMessage( respondable, entity.GetType(), entity, methodName, parameters );
	}

	/// <summary>
	/// Creates an RPC call message for a static method.
	/// </summary>
	/// <param name="respondable">Whether or not the RPC is expecting a response.</param>
	/// <param name="type">The type that holds the method.</param>
	/// <param name="methodName">The name of the method to call.</param>
	/// <param name="parameters">The parameters to pass to the method.</param>
	/// <returns>The built RPC message.</returns>
	private static RpcCallMessage CreateRpc( bool respondable, Type type, string methodName, INetworkable[] parameters )
	{
		return new RpcCallMessage( respondable, type, null, methodName, parameters );
	}

	/// <summary>
	/// Awaits for a response on the specific RPC with the provided <see cref="Guid"/>.
	/// </summary>
	/// <param name="callGuid">The <see cref="Guid"/> to wait for a response on.</param>
	/// <returns>The response for the call.</returns>
	private static async Task<RpcCallResponseMessage> WaitForResponseAsync( Guid callGuid )
	{
		// TODO: Surely there's a better way to do this right?
		// TODO: This does not account for disconnects or the environment shutting down.
		while ( !RpcResponses.ContainsKey( callGuid  ) )
			await Task.Delay( 1 );
		
		RpcResponses.Remove( callGuid, out var response );
		if ( response is null )
		{
			Logging.Error( $"Failed to return RPC response (\"{callGuid}\" became invalid unexpectedly)." );
			return default!;
		}
		
		return response;
	}

	/// <summary>
	/// Marks a method to be a client-side RPC.
	/// </summary>
	[AttributeUsage( AttributeTargets.Method )]
	public class ClientAttribute : Attribute
	{
		/// <summary>
		/// The amount of times the server can execute the RPC per second.
		/// </summary>
		public double LimitPerSecond;

		public ClientAttribute( double limitPerSecond = double.MaxValue )
		{
			LimitPerSecond = limitPerSecond;
		}
	}

	/// <summary>
	/// Marks a method to be a server-side RPC.
	/// </summary>
	[AttributeUsage( AttributeTargets.Method )]
	public class ServerAttribute : Attribute
	{
		/// <summary>
		/// The amount of times a client can execute the RPC per second.
		/// </summary>
		public double LimitPerSecond;

		public ServerAttribute( double limitPerSecond = double.MaxValue )
		{
			
		}
	}
}
#endif
