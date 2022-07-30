using System;
using System.Collections.Generic;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Server;

public class BotClient : INetworkClient
{
	private static readonly Dictionary<Type, Action<BotClient, NetworkMessage>> MessageHandlers = new();
	
	public long ClientId { get; }
	public NetworkEntity? Pawn { get; set; }
	
	public BotClient( long clientId )
	{
		ClientId = clientId;
	}

	public void SendMessage( byte[] bytes )
	{
		throw new NotImplementedException();
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

	public static void HandleBotMessage<T>( Action<BotClient, NetworkMessage> cb ) where T : NetworkMessage
	{
		var messageType = typeof(T);
		if ( MessageHandlers.ContainsKey( messageType ) )
			throw new Exception( $"Message type {messageType} is already being handled." );

		MessageHandlers.Add( messageType, cb );
	}
}
