using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Utility;
using vtortola.WebSockets;
using vtortola.WebSockets.Http;
using vtortola.WebSockets.Rfc6455;

namespace CustomNetworking.Server;

public static class NetworkManager
{
#if DEBUG
	public static int MessagesSent;
	public static int MessagesSentToClients;
#endif
	
	public static ConcurrentDictionary<long, INetworkClient> Clients { get; } = new();
	public static ConcurrentDictionary<long, BotClient> Bots { get; } = new();

	public delegate void ClientConnectedEventHandler( INetworkClient client );
	public static event ClientConnectedEventHandler? ClientConnected;
	
	public delegate void ClientDisconnectedEventHandler( INetworkClient client );
	public static event ClientDisconnectedEventHandler? ClientDisconnected;
	
	private static readonly Dictionary<Type, Action<INetworkClient, NetworkMessage>> MessageHandlers = new();
	private static readonly ConcurrentQueue<(To, NetworkMessage)> OutgoingQueue = new();
	private static readonly ConcurrentQueue<(INetworkClient, NetworkMessage)> IncomingQueue = new();

	public static async void NetworkingMain()
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
		await server.StopAsync();
	}
	
	private static Task<bool> HttpAuthenticationHandler( WebSocketHttpRequest request, WebSocketHttpResponse response )
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

	private static async Task AcceptWebSocketClientsAsync( WebSocketListener server, CancellationToken token )
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

	public static async Task AbandonClient( ClientSocket clientSocket )
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

	private static void AcceptClient( INetworkClient client )
	{
		Clients.TryAdd( client.ClientId, client );
		if ( client is BotClient bot )
			Bots.TryAdd( bot.ClientId, bot );
		
		ClientConnected?.Invoke( client );
	}

	public static void AcceptClient( long clientId, ClientSocket clientSocket )
	{
		AcceptClient( new NetworkClient( clientId, clientSocket ) );
	}

	public static void AcceptClient( long clientId )
	{
		AcceptClient( new BotClient( clientId ) );
	}

	public static void SendOutgoing()
	{
		while ( OutgoingQueue.TryDequeue( out var pair ) )
			SendMessage( pair.Item1, pair.Item2 );
	}

	private static void SendMessage( To to, NetworkMessage message )
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

	public static void QueueMessage( To to, NetworkMessage message )
	{
		OutgoingQueue.Enqueue( (to, message) );
	}

	public static void DispatchIncoming()
	{
		while ( IncomingQueue.TryDequeue( out var pair ) )
		{
			if ( !MessageHandlers.TryGetValue( pair.Item2.GetType(), out var cb ) )
				throw new Exception( $"Unhandled message {pair.Item2.GetType()}." );
		
			cb.Invoke( pair.Item1, pair.Item2 );	
		}
	}

	public static void QueueIncoming( INetworkClient client, NetworkMessage message )
	{
		IncomingQueue.Enqueue( (client, message) );
	}

	public static void HandleMessage<T>( Action<INetworkClient, NetworkMessage> cb ) where T : NetworkMessage
	{
		var messageType = typeof(T);
		if ( MessageHandlers.ContainsKey( messageType ) )
			throw new Exception( $"Message type {messageType} is already being handled." );

		MessageHandlers.Add( messageType, cb );
	}

	public static INetworkClient? GetClientById( long playerId )
	{
		return playerId == -1 ? null : Clients[playerId];
	}
}
