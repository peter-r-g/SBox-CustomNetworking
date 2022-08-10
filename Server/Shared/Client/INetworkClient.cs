using System.Collections.Generic;
#if CLIENT
using System;
using CustomNetworking.Client;
#endif
#if SERVER
using CustomNetworking.Server;
using CustomNetworking.Shared.Messages;
#endif
using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Shared;

/// <summary>
/// Contract to define something that is a client that can connect to a server.
/// </summary>
public interface INetworkClient
{
	/// <summary>
	/// 
	/// </summary>
	delegate void PawnChangedEventHandler( INetworkClient client, IEntity? oldPawn, IEntity? newPawn );
	/// <summary>
	/// 
	/// </summary>
	event PawnChangedEventHandler? PawnChanged;
	
	/// <summary>
	/// The unique identifier of the client.
	/// </summary>
	long ClientId { get; }

	/// <summary>
	/// The player entity that the client is controlling.
	/// </summary>
	IEntity? Pawn { get; set; }

#if SERVER
	/// <summary>
	/// Sends an array of bytes to the client.
	/// </summary>
	/// <param name="bytes">The data to send to the client.</param>
	void SendMessage( byte[] bytes );
	/// <summary>
	/// Serializes a message and sends the data to the client.
	/// </summary>
	/// <param name="message">The message to send to the client.</param>
	void SendMessage( NetworkMessage message );
	
	/// <summary>
	/// Contains all currently connected players in the server.
	/// </summary>
	public static IReadOnlyDictionary<long, INetworkClient> All => NetworkServer.Instance.Clients;
#endif

#if CLIENT
	/// <summary>
	/// Contains all currently connected players in the server.
	/// <remarks>This may not actually contain all connected clients as the server could be limiting this information.</remarks>
	/// </summary>
	public static IReadOnlyDictionary<long, INetworkClient> All
	{
		get
		{
			if ( NetworkManager.Instance is null )
				throw new Exception( "Attempted to access all clients when the NetworkManager doesn't exist." );
			
			return NetworkManager.Instance.Clients;
		}
	}
#endif
}
