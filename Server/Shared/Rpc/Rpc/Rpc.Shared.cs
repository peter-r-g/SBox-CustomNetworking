#if !MONITOR
using System;
#if SERVER
using System.Collections.Generic;
#endif
using System.Threading.Tasks;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.RemoteProcedureCalls;

/// <summary>
/// A collection of methods to execute Remote Procedure Calls (RPCs).
/// </summary>
public static partial class Rpc
{
	private static RpcCallMessage CreateRpc( bool respondable, IEntity entity, string methodName, string? componentName,
		INetworkable[] parameters )
	{
		return new RpcCallMessage( respondable, entity.GetType(), entity, methodName, componentName, parameters );
	}

	private static RpcCallMessage CreateRpc( bool respondable, Type type, string methodName, INetworkable[] parameters )
	{
		return new RpcCallMessage( respondable, type, null, methodName, null, parameters );
	}

	private static async Task<RpcCallResponseMessage> WaitForResponseAsync( Guid callGuid )
	{
		// TODO: Surely there's a better way to do this right?
		// TODO: This does not account for disconnects or the environment shutting down.
		while ( !RpcResponses.ContainsKey( callGuid  ) )
			await Task.Delay( 1 );
		
		RpcResponses.Remove( callGuid, out var response );
		if ( response is null )
		{
			var exception = new InvalidOperationException();
			Logging.Error( $"Failed to return RPC response (\"{callGuid}\" became invalid unexpectedly).", exception );
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
