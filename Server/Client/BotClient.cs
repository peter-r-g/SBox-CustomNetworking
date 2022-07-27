using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Messages;

namespace CustomNetworking.Server;

public class BotClient : INetworkClient
{
	private static readonly Dictionary<Type, Action<BotClient, NetworkMessage>> MessageHandlers = new();
	
	public long ClientId { get; }
	public NetworkEntity? Pawn { get; set; }

	private readonly int _speakIntervalMs = Random.Shared.Next( 10000, 120000 );
	private Task _speakTask = Task.CompletedTask;
	
	public BotClient( long clientId )
	{
		ClientId = clientId;
	}

	public void Think()
	{
		if ( _speakTask.IsCompleted )
			_speakTask = Task.Run( SpeakAsync );
	}

	private async Task SpeakAsync()
	{
		await Task.Delay( _speakIntervalMs );
		NetworkManager.QueueIncoming( this, new SayMessage( null, "Bot go brr" ) );
	}

	public void SendMessage( byte[] bytes )
	{
		throw new NotImplementedException();
	}

	public void SendMessage( NetworkMessage message )
	{
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
