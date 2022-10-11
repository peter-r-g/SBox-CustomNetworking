using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetBolt.Shared;
using NetBolt.Shared.Entities;
using NetBolt.Shared.Messages;
using NetBolt.Shared.Utility;
using NetBolt.WebSocket.Enums;

namespace NetBolt.Server;

/// <summary>
/// A bot client.
/// </summary>
public sealed class BotClient : INetworkClient
{
	/// <summary>
	/// A bots own client-side message handlers.
	/// </summary>
	private static readonly Dictionary<Type, Action<BotClient, NetworkMessage>> MessageHandlers = new();
	public event INetworkClient.PawnChangedEventHandler? PawnChanged;
	
	public long ClientId { get; }
	public bool IsBot => true;
	
	public bool Connected => true;
	public bool ConnectedAndUpgraded => true;
	public int Ping => 0;
	
	public IEntity? Pawn
	{
		get => _pawn;
		set
		{
			if ( value is not null && _pawn is not null )
				return;
			
			if ( value is not null && _pawn is not null && value.EntityId == _pawn.EntityId )
				return;
			
			var oldPawn = _pawn;
			_pawn = value;
			PawnChanged?.Invoke( this, oldPawn, _pawn );
		}
	}
	private IEntity? _pawn;

	internal BotClient( long clientId )
	{
		ClientId = clientId;
	}
	
	public void QueueSend( NetworkMessage message )
	{
		if ( !MessageHandlers.TryGetValue( message.GetType(), out var cb ) )
		{
			Logging.Error( $"Unhandled message type {message.GetType()} for bot." );
			return;
		}
		
		cb.Invoke( this, message );
	}

	public override string ToString()
	{
		return $"Bot (ID: {ClientId})";
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
		{
			Logging.Error( $"Message type {messageType} is already being handled for bots." );
			return;
		}

		MessageHandlers.Add( messageType, cb );
	}

	public Task DisconnectAsync( WebSocketDisconnectReason reason = WebSocketDisconnectReason.Requested, string strReason = "",
		WebSocketError? error = null )
	{
		throw new NotImplementedException();
	}

	public Task HandleAsync()
	{
		return Task.CompletedTask;
	}

	public ValueTask<int> PingAsync( int timeout = int.MaxValue )
	{
		return new ValueTask<int>( 0 );
	}

	public void QueueSend( byte[] bytes )
	{
		throw new NotImplementedException();
	}

	public void QueueSend( string message )
	{
		throw new NotImplementedException();
	}
}
