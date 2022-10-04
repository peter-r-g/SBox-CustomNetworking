﻿using System.Collections.Generic;
#if CLIENT
using System;
using CustomNetworking.Client;
using CustomNetworking.Shared.Utility;
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
	/// The delegate for handling when <see cref="INetworkClient.PawnChanged"/> has been invoked.
	/// </summary>
	delegate void PawnChangedEventHandler( INetworkClient client, IEntity? oldPawn, IEntity? newPawn );
	/// <summary>
	/// Called when a clients pawn has changed.
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
	/// <remarks>This contains all bots as well.</remarks>
	/// </summary>
	public static IReadOnlyDictionary<long, INetworkClient> All => NetworkServer.Instance.Clients;

	/// <summary>
	/// Contains all currently connected bots in the server.
	/// </summary>
	public static IReadOnlyDictionary<long, BotClient> Bots => NetworkServer.Instance.Bots;
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
			{
				Logging.Error( $"Attempted to access all clients when the {nameof(NetworkManager)} doesn't exist." );
				return null!;
			}
			
			return NetworkManager.Instance.Clients;
		}
	}

	/// <summary>
	/// Gets the local client in the server.
	/// </summary>
	public static INetworkClient Local
	{
		get
		{
			if ( NetworkManager.Instance is null )
			{
				Logging.Error( $"Attempted to access local client when the {nameof(NetworkManager)} doesn't exist." );
				return null!;
			}
			
			return NetworkManager.Instance.LocalClient;
		}
	}
#endif
}