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
/// Handles a web socket server, its clients, and dispatches messages to and from them.
/// </summary>
public sealed class NetworkServer
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
	
	/// <summary>
	/// A dictionary containing all connected clients.
	/// <remarks>This dictionary includes bots.</remarks>
	/// </summary>
	internal ConcurrentDictionary<long, INetworkClient> Clients { get; } = new();
	/// <summary>
	/// A dictionary containing all connected bots.
	/// </summary>
	internal ConcurrentDictionary<long, BotClient> Bots { get; } = new();
	
	/// <summary>
	/// The handlers for incoming messages.
	/// </summary>
	private readonly Dictionary<Type, Action<INetworkClient, NetworkMessage>> _messageHandlers = new();
	/// <summary>
	/// The queue for messages outgoing to clients.
	/// </summary>
	private readonly ConcurrentQueue<(To, NetworkMessage)> _outgoingQueue = new();
	/// <summary>
	/// The queue for messages incoming to the server.
	/// </summary>
	private readonly ConcurrentQueue<(INetworkClient, NetworkMessage)> _incomingQueue = new();

	internal NetworkServer( int port, bool useSteam )
	{
		Instance = this;
		
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
			Logging.Error( $"Message type {messageType} is already being handled." );
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
	
	/// <summary>
	/// Starts the network server and closes once the programs token is cancelled.
	/// </summary>
	internal async Task Start()
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
		await clientAcceptTask;
		
		SendMessage( To.All, new ShutdownMessage() );
		await server.StopAsync();
	}
	
	/// <summary>
	/// The handler to authenticate incoming client web sockets.
	/// </summary>
	/// <param name="request">The clients web socket request.</param>
	/// <param name="response">The response to send to the client.</param>
	/// <returns>Whether or not the connection should be accepted.</returns>
	private async Task<bool> HttpAuthenticationHandler( WebSocketHttpRequest request, WebSocketHttpResponse response )
	{
		var userAgent = request.Headers.Get( RequestHeader.UserAgent );
		if ( userAgent != "facepunch-s&box" )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return false;
		}
					
		var origin = request.Headers.Get( RequestHeader.Origin );
		if ( origin != "https://sbox.facepunch.com/" )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return false;
		}

		var version = request.Headers.Get( RequestHeader.WebSocketVersion );
		if ( version != "13" )
		{
			response.Status = HttpStatusCode.Unauthorized;
			return false;
		}

		if ( UsingSteam )
		{
			var steam = request.Headers.Get( "Steam" );
			if ( !long.TryParse( steam, out var clientId ) || Clients.TryGetValue( clientId, out _ ) )
			{
				response.Status = HttpStatusCode.Unauthorized;
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Asynchronous task to accept any incoming clients.
	/// </summary>
	/// <param name="server">The web socket server.</param>
	/// <param name="token">The token to watch for when it is cancelled.</param>
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
	
	/// <summary>
	/// Drops a client from the game.
	/// </summary>
	/// <param name="clientSocket">The client socket to drop.</param>
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
				BaseGame.Current.OnClientDisconnected( client );
		}
		
		await clientSocket.CloseAsync();
	}
	
	/// <summary>
	/// Accepts a new client to the server.
	/// </summary>
	/// <param name="clientId">The unique identifier of the client.</param>
	/// <param name="clientSocket">The socket the client is using.</param>
	internal void AcceptClient( long clientId, ClientSocket clientSocket )
	{
		AcceptClient( new NetworkClient( clientId, clientSocket ) );
	}
	
	/// <summary>
	/// Accepts a bot to the server.
	/// </summary>
	/// <param name="clientId">The unique identifier of the bot.</param>
	internal void AcceptClient( long clientId )
	{
		AcceptClient( new BotClient( clientId ) );
	}
	
	/// <summary>
	/// Accepts a network client to the server.
	/// </summary>
	/// <param name="client">The network client to accept.</param>
	private void AcceptClient( INetworkClient client )
	{
		Clients.TryAdd( client.ClientId, client );
		if ( client is BotClient bot )
			Bots.TryAdd( bot.ClientId, bot );
		
		BaseGame.Current.OnClientConnected( client );
	}
	
	/// <summary>
	/// Dispatches any incoming server messages.
	/// </summary>
	internal void DispatchIncoming()
	{
		while ( _incomingQueue.TryDequeue( out var pair ) )
		{
			if ( !_messageHandlers.TryGetValue( pair.Item2.GetType(), out var cb ) )
			{
				Logging.Error( $"Unhandled message type {pair.Item2.GetType()}." );
				continue;
			}
		
			cb.Invoke( pair.Item1, pair.Item2 );	
		}
	}

	/// <summary>
	/// Dispatches any outgoing messages to clients.
	/// </summary>
	internal void DispatchOutgoing()
	{
		while ( _outgoingQueue.TryDequeue( out var pair ) )
			SendMessage( pair.Item1, pair.Item2 );
	}

	/// <summary>
	/// Sends a message to clients.
	/// </summary>
	/// <param name="to">The clients to send the message to.</param>
	/// <param name="message">The message to send.</param>
	private void SendMessage( To to, NetworkMessage message )
	{
		MessagesSent++;
		
		// Quick send message to bots.
		foreach ( var client in to )
		{
			if ( client is BotClient )
				client.SendMessage( message );
		}
		
		// Write message once.
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkable( message );
		var numBytes = stream.Length;
		writer.Close();
		var bytes = stream.ToArray();

		// If message is less than max buffer size then send this to all clients.
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

		// Break down message to many messages and send.
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
