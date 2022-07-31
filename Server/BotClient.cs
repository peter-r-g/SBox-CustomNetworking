﻿using System;
using System.Collections.Generic;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

public class BotClient : INetworkClient
{
	private static readonly Dictionary<Type, Action<BotClient, NetworkMessage>> MessageHandlers = new();
	
	public long ClientId { get; }
	
	internal BotClient( long clientId )
	{
		ClientId = clientId;
	}

	public void SendMessage( byte[] bytes )
	{
		throw new InvalidOperationException();
	}

	public void SendMessage( NetworkMessage message )
	{
#if DEBUG
		NetworkServer.Instance.MessagesSentToClients++;
#endif
		if ( !MessageHandlers.TryGetValue( message.GetType(), out var cb ) )
			throw new Exception( $"Unhandled message {message.GetType()}." );
		
		cb.Invoke( this, message );
	}

	/// <summary>
	/// Adds a handler for the bot to dispatch the message to.
	/// </summary>
	/// <param name="cb">The method to call when a message of type <see cref="T"/> has come in.</param>
	/// <typeparam name="T">The message type to handle.</typeparam>
	/// <exception cref="Exception">Thrown when a handler has already been set for <see cref="T"/>.</exception>
	public static void HandleBotMessage<T>( Action<BotClient, NetworkMessage> cb ) where T : NetworkMessage
	{
		var messageType = typeof(T);
		if ( MessageHandlers.ContainsKey( messageType ) )
			throw new Exception( $"Message type {messageType} is already being handled." );

		MessageHandlers.Add( messageType, cb );
	}
}
