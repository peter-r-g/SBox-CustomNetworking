using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;
using CustomNetworking.Shared.Utility;
using vtortola.WebSockets;
using vtortola.WebSockets.Http;
using vtortola.WebSockets.Rfc6455;

namespace CustomNetworking.Server;

public sealed class NetworkServer
{
	public static NetworkServer Instance = null!;
	
#if DEBUG
	public int MessagesReceived;
	public int MessagesSent;
	public int MessagesSentToClients;
#endif
	
	public ConcurrentDictionary<long, INetworkClient> Clients { get; } = new();
	public ConcurrentDictionary<long, BotClient> Bots { get; } = new();

	internal delegate void ClientConnectedEventHandler( INetworkClient client );
	internal event ClientConnectedEventHandler? ClientConnected;
	
	internal delegate void ClientDisconnectedEventHandler( INetworkClient client );
	internal event ClientDisconnectedEventHandler? ClientDisconnected;
	
	private readonly Dictionary<Type, Action<INetworkClient, NetworkMessage>> _messageHandlers = new();
	private readonly ConcurrentQueue<(To, NetworkMessage)> _outgoingQueue = new();
	private readonly ConcurrentQueue<(INetworkClient, NetworkMessage)> _incomingQueue = new();

	public NetworkServer()
	{
		if ( Instance is not null )
			throw new Exception( $"An instance of {nameof(NetworkServer)} already exists" );
		
		Instance = this;
	}
	public void QueueMessage( To to, NetworkMessage message )
	{
		_outgoingQueue.Enqueue( (to, message) );
	}
	public void QueueIncoming( INetworkClient client, NetworkMessage message )
	{
		_incomingQueue.Enqueue( (client, message) );
	}

	public void HandleMessage<T>( Action<INetworkClient, NetworkMessage> cb ) where T : NetworkMessage
	{
		var messageType = typeof(T);
		if ( _messageHandlers.ContainsKey( messageType ) )
			throw new Exception( $"Message type {messageType} is already being handled." );

		_messageHandlers.Add( messageType, cb );
	}
	public INetworkClient? GetClientById( long clientId )
	{
		return Clients.ContainsKey( clientId ) ? Clients[clientId] : null;
	}
	
	internal async void NetworkingMain()
	{
		var options = new WebSocketListenerOptions
		{
			HttpAuthenticationHandler = HttpAuthenticationHandler,
			PingTimeout = TimeSpan.FromSeconds( 5 )
		};
		options.Standards.Add( new WebSocketFactoryRfc6455() );
		options.Transports.ConfigureTcp( tcp =>
		{
			tcp.BacklogSize = 100;
			tcp.ReceiveBufferSize = SharedConstants.MaxBufferSize;
			tcp.SendBufferSize = SharedConstants.MaxBufferSize;
		} );
		
		var server = new WebSocketListener( new IPEndPoint( IPAddress.Any, SharedConstants.Port ), options );
		await server.StartAsync();
		var clientAcceptTask = Task.Run( () => AcceptWebSocketClientsAsync( server, Program.ProgramCancellation.Token ) );

		while ( !Program.ProgramCancellation.IsCancellationRequested )
		{
		}

		clientAcceptTask.Wait();
		SendMessage( To.All, new ShutdownMessage() );
		await server.StopAsync();
	}
	
	private Task<bool> HttpAuthenticationHandler( WebSocketHttpRequest request, WebSocketHttpResponse response )
	{
		var userAgent = request.Headers.Get( RequestHeader.UserAgent );
		if ( userAgent != "facepunch-s&box" )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return Task.FromResult( false );
		}
					
		var origin = request.Headers.Get( RequestHeader.Origin );
		if ( origin != "https://sbox.facepunch.com/" )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return Task.FromResult( false );
		}

		var version = request.Headers.Get( RequestHeader.WebSocketVersion );
		if ( version != "13" )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return Task.FromResult( false );
		}

		var steam = request.Headers.Get( "Steam" );
		if ( !long.TryParse( steam, out var clientId ) || Clients.TryGetValue( clientId, out _ ) )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return Task.FromResult( false );
		}
					
		return Task.FromResult( true );
	}

	private async Task AcceptWebSocketClientsAsync( WebSocketListener server, CancellationToken token )
	{
		while ( !token.IsCancellationRequested )
		{
			try
			{
				var ws = await server.AcceptWebSocketAsync( token ).ConfigureAwait( false );
				if ( ws is null )
					continue;
				
				var client = new ClientSocket( ws );
				_ = Task.Run( () => client.HandleConnectionAsync(), client.ClientTokenSource.Token );
			}
			catch ( Exception e )
			{
				Program.Logger.Enqueue( e.ToString() );
			}
		}
	}
	
	internal async Task AbandonClient( ClientSocket clientSocket )
	{
		// TODO: This needs to be better.
		long? idToRemove = null;
		foreach ( var pair in Clients )
		{
			if ( pair.Value is not NetworkClient client )
				continue;
			
			if ( client.ClientSocket != clientSocket )
				continue;

			idToRemove = pair.Key;
			break;
		}

		if ( idToRemove is not null )
		{
			if ( Clients.TryRemove( idToRemove.Value, out var client ) )
				ClientDisconnected?.Invoke( client );
		}
		
		await clientSocket.CloseAsync();
	}
	
	internal void AcceptClient( long clientId, ClientSocket clientSocket )
	{
		AcceptClient( new NetworkClient( clientId, clientSocket ) );
	}
	
	internal void AcceptClient( long clientId )
	{
		AcceptClient( new BotClient( clientId ) );
	}
	
	private void AcceptClient( INetworkClient client )
	{
		Clients.TryAdd( client.ClientId, client );
		if ( client is BotClient bot )
			Bots.TryAdd( bot.ClientId, bot );
		
		ClientConnected?.Invoke( client );
	}
	
	internal void DispatchIncoming()
	{
		while ( _incomingQueue.TryDequeue( out var pair ) )
		{
			if ( !_messageHandlers.TryGetValue( pair.Item2.GetType(), out var cb ) )
				throw new Exception( $"Unhandled message {pair.Item2.GetType()}." );
		
			cb.Invoke( pair.Item1, pair.Item2 );	
		}
	}

	internal void DispatchOutgoing()
	{
		while ( _outgoingQueue.TryDequeue( out var pair ) )
			SendMessage( pair.Item1, pair.Item2 );
	}

	private void SendMessage( To to, NetworkMessage message )
	{
#if DEBUG
		MessagesSent++;
#endif
		
		foreach ( var client in to )
		{
			if ( client is BotClient )
				client.SendMessage( message );
		}
		
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkable( message );
		var numBytes = stream.Length;
		writer.Close();

		if ( numBytes <= SharedConstants.MaxBufferSize )
		{
			foreach ( var client in to )
			{
				if ( client is BotClient )
					continue;
				
				client.SendMessage( stream.ToArray() );
			}
			
			return;
		}

		var partialMessages = NetworkMessage.Split( stream.ToArray() );
		foreach ( var client in to )
		{
			if ( client is BotClient )
				continue;
			
			foreach ( var partialMessage in partialMessages )
				client.SendMessage( partialMessage );
		}
	}
}
