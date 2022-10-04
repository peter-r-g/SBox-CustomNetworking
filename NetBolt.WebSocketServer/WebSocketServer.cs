using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetBolt.WebSocket.Enums;
using NetBolt.WebSocket.Utility;

namespace NetBolt.WebSocket;

/// <summary>
/// A basic implementation of a web socket server wrapper around a <see cref="TcpListener"/>.
/// </summary>
public class WebSocketServer : IWebSocketServer
{
	/// <summary>
	/// A read-only list of all clients that are connected to the server.
	/// </summary>
	public IReadOnlyList<IWebSocketClient> Clients => ClientSockets;
	/// <summary>
	/// A read-only list of all clients that are connected to the server and have been upgraded to the web socket protocol.
	/// </summary>
	public IReadOnlyList<IWebSocketClient> UpgradedClients => ClientSockets.Where( client => client.ConnectedAndUpgraded ).ToList();
	
	/// <summary>
	/// A list of all clients that are connected to the server.
	/// </summary>
	private List<IWebSocketClient> ClientSockets { get; } = new();

	/// <summary>
	/// Whether or not the server is running.
	/// </summary>
	public bool Running { get; private set; }
	/// <summary>
	/// Whether or not a stop to the server has been requested.
	/// </summary>
	protected bool StopRequested { get; private set; }
	
	/// <summary>
	/// The underlying server accepting connections.
	/// </summary>
	private readonly TcpListener _server;
	/// <summary>
	/// The asynchronous task to accept clients to the server.
	/// </summary>
	private Task? _acceptClientsTask;
	/// <summary>
	/// A dictionary containing a map of clients to their handling task.
	/// </summary>
	private readonly Dictionary<IWebSocketClient, Task> _clientTasks = new();
	
	public WebSocketServer( IPAddress ipAddress, int port )
	{
		_server = new TcpListener( ipAddress, port );
	}

	/// <summary>
	/// Starts the server.
	/// </summary>
	public void Start()
	{
		this.ThrowIfRunning();

		_server.Start();
		_acceptClientsTask = AcceptClientsAsync();
		Running = true;
	}

	/// <summary>
	/// Stops the server.
	/// </summary>
	/// <returns>The async task that spawns from the invoke.</returns>
	public async Task StopAsync()
	{
		this.ThrowIfNotRunning();
		
		StopRequested = true;
		var tasks = new List<Task>();
		if ( _acceptClientsTask is not null )
			tasks.Add( _acceptClientsTask );
		
		tasks.AddRange( _clientTasks.Values );
		var clients = ClientSockets.ToImmutableArray();
		foreach ( var client in clients )
			tasks.Add( DisconnectClientAsync( client, WebSocketDisconnectReason.ServerShutdown ) );

		await Task.WhenAll( tasks ).ConfigureAwait( false );
		
		_clientTasks.Clear();
		_server.Stop();
		Running = false;
		StopRequested = false;
	}

	/// <summary>
	/// Accepts a client to the server.
	/// </summary>
	/// <param name="client">The client to accept.</param>
	public virtual void AcceptClient( IWebSocketClient client )
	{
		this.ThrowIfNotRunning();

		ClientSockets.Add( client );
		_clientTasks.Add( client, client.HandleClientAsync() );
		OnClientConnected( client );
	}

	/// <summary>
	/// Disconnects a client from the server.
	/// </summary>
	/// <param name="client">The client to disconnect/</param>
	/// <param name="reason">The reason for the disconnect</param>
	/// <returns>The async task that spawns from the invoke.</returns>
	public async Task DisconnectClientAsync( IWebSocketClient client, WebSocketDisconnectReason reason = WebSocketDisconnectReason.Requested )
	{
		this.ThrowIfNotRunning();
		
		if ( !ClientSockets.Contains( client ) )
			return;

		await client.DisconnectAsync( reason ).ConfigureAwait( false );
	}

	/// <summary>
	/// Invoked when a client has connected to the server.
	/// <remarks>This does not mean they are capable of receiving messages yet. See <see cref="OnClientUpgraded"/>.</remarks>
	/// </summary>
	/// <param name="client">The client that has connected.</param>
	public virtual void OnClientConnected( IWebSocketClient client )
	{
	}

	/// <summary>
	/// Invoked when a client has been upgraded to the web socket protocol.
	/// </summary>
	/// <param name="client">The client that has been upgraded.</param>
	public virtual void OnClientUpgraded( IWebSocketClient client )
	{
	}

	/// <summary>
	/// Invoked when a client has disconnected from the server.
	/// </summary>
	/// <param name="client">The client that has disconnected.</param>
	/// <param name="reason">The reason for the client disconnecting.</param>
	/// <param name="error">The error associated with the disconnect.</param>
	public virtual void OnClientDisconnected( IWebSocketClient client, WebSocketDisconnectReason reason, WebSocketError? error )
	{
		ClientSockets.Remove( client );
		_clientTasks.Remove( client );
	}

	/// <summary>
	/// Sends a <see cref="WebSocketOpCode.Binary"/> message to clients.
	/// </summary>
	/// <param name="to">The clients to send the message to.</param>
	/// <param name="bytes">The binary data to send.</param>
	public virtual void Send( To to, byte[] bytes )
	{
		this.ThrowIfNotRunning();

		foreach ( var client in to )
			client.Send( bytes );
	}

	/// <summary>
	/// Sends a <see cref="WebSocketOpCode.Text"/> message to clients.
	/// </summary>
	/// <param name="to">The clients to send the message to.</param>
	/// <param name="message">The message to send.</param>
	public virtual void Send( To to, string message )
	{
		this.ThrowIfNotRunning();
		
		foreach ( var client in to )
			client.Send( message );
	}

	/// <summary>
	/// Creates a socket client for the provided <see cref="TcpClient"/>.
	/// </summary>
	/// <param name="client">The client socket to create a wrapper around.</param>
	/// <returns>The created socket client.</returns>
	protected virtual IWebSocketClient CreateClient( TcpClient client )
	{
		return new WebSocketClient( client, this );
	}

	/// <summary>
	/// The asynchronous handler for accepting clients to the server.
	/// </summary>
	private async Task AcceptClientsAsync()
	{
		while ( !StopRequested )
		{
			try
			{
				var tokenSource = new CancellationTokenSource();
				tokenSource.CancelAfter( TimeSpan.FromMilliseconds( 500 ) );
				var client = await _server.AcceptTcpClientAsync( tokenSource.Token ).ConfigureAwait( false );
				if ( StopRequested )
					return;

				var socketClient = CreateClient( client );
				AcceptClient( socketClient );
			}
			catch ( OperationCanceledException )
			{
			}
		}
	}
}
