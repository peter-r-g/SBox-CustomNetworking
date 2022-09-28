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

/// <summary>
/// Handles a web socket server, its clients, and dispatches messages from them.
/// </summary>
public class NetworkServer
{
	/// <summary>
	/// The game server instance.
	/// </summary>
	public static NetworkServer Instance { get; internal set; } = null!;
	
	/// <summary>
	/// Debug stat for how many messages have been received from clients.
	/// <remarks>This does not account for any messages from a <see cref="BotClient"/>.</remarks>
	/// </summary>
	public int MessagesReceived { get; internal set; }
	/// <summary>
	/// Debug stat for how messages have been sent to clients.
	/// <remarks>This does account for any <see cref="BotClient"/>s connected.</remarks>
	/// </summary>
	public int MessagesSent { get; internal set; }
	/// <summary>
	/// Debug stat for how many individual messages have been sent to any clients.
	/// <remarks>This does account for any <see cref="BotClient"/>s connected.</remarks>
	/// </summary>
	public int MessagesSentToClients { get; internal set; }
	
	/// <summary>
	/// The port that the server is listening on.
	/// </summary>
	public int Port { get; }
	
	/// <summary>
	/// Whether or not steam verification is being used.
	/// </summary>
	public bool UsingSteam { get; }
	
	internal ConcurrentDictionary<long, INetworkClient> Clients { get; } = new();
	internal ConcurrentDictionary<long, BotClient> Bots { get; } = new();

	internal delegate void ClientConnectedEventHandler( INetworkClient client );
	internal event ClientConnectedEventHandler? ClientConnected;
	
	internal delegate void ClientDisconnectedEventHandler( INetworkClient client );
	internal event ClientDisconnectedEventHandler? ClientDisconnected;
	
	private readonly Dictionary<Type, Action<INetworkClient, NetworkMessage>> _messageHandlers = new();
	private readonly ConcurrentQueue<(To, NetworkMessage)> _outgoingQueue = new();
	private readonly ConcurrentQueue<(INetworkClient, NetworkMessage)> _incomingQueue = new();

	internal NetworkServer( int port, bool useSteam )
	{
		Port = port;
		UsingSteam = useSteam;
	}
	
	/// <summary>
	/// Queues a message to be sent to clients.
	/// </summary>
	/// <param name="to">The client(s) to send the message to.</param>
	/// <param name="message">The message to send to each client.</param>
	public void QueueMessage( To to, NetworkMessage message )
	{
		_outgoingQueue.Enqueue( (to, message) );
	}

	/// <summary>
	/// Queues a message to be processed by the server.
	/// </summary>
	/// <remarks>This should only be used in cases where a <see cref="BotClient"/> is doing something.</remarks>
	/// <param name="client">The client that sent the message.</param>
	/// <param name="message">The message the client has sent.</param>
	public void QueueIncoming( INetworkClient client, NetworkMessage message )
	{
		_incomingQueue.Enqueue( (client, message) );
	}

	/// <summary>
	/// Adds a handler for the server to dispatch the message to.
	/// </summary>
	/// <param name="cb">The method to call when a message of type <see cref="T"/> has come in.</param>
	/// <typeparam name="T">The message type to handle.</typeparam>
	/// <exception cref="Exception">Thrown when a handler has already been set for <see cref="T"/>.</exception>
	public void HandleMessage<T>( Action<INetworkClient, NetworkMessage> cb ) where T : NetworkMessage
	{
		var messageType = typeof(T);
		if ( _messageHandlers.ContainsKey( messageType ) )
		{
			Logging.Error( $"Message type {messageType} is already being handled.", new InvalidOperationException() );
			return;
		}

		_messageHandlers.Add( messageType, cb );
	}

	/// <summary>
	/// Gets a client that is connected to the server.
	/// </summary>
	/// <param name="clientId">The ID of the client to get.</param>
	/// <returns>The client that was found. Null if no client was found.</returns>
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
		
		var server = new WebSocketListener( new IPEndPoint( IPAddress.Any, Port ), options );
		await server.StartAsync();
		var clientAcceptTask = Task.Run( () => AcceptWebSocketClientsAsync( server, Program.ProgramCancellation.Token ) );

		// TODO: Cooking the CPU is not a very cool way of doing this
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

		if ( UsingSteam )
		{
			var steam = request.Headers.Get( "Steam" );
			if ( !long.TryParse( steam, out var clientId ) || Clients.TryGetValue( clientId, out _ ) )
			{
				response.Status = HttpStatusCode.Unauthorized;
				return Task.FromResult( false );
			}
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
				Logging.Error( e );
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
			{
				Logging.Error( $"Unhandled message type {pair.Item2.GetType()}.", new InvalidOperationException() );
				continue;
			}
		
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
		MessagesSent++;
		
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
		var bytes = stream.ToArray();

		if ( numBytes <= SharedConstants.MaxBufferSize )
		{
			foreach ( var client in to )
			{
				if ( client is BotClient )
					continue;
				
				client.SendMessage( bytes );
			}
			
			return;
		}

		var partialMessages = NetworkMessage.Split( bytes );
		foreach ( var client in to )
		{
			if ( client is BotClient )
				continue;
			
			foreach ( var partialMessage in partialMessages )
				client.SendMessage( partialMessage );
		}
	}
}
