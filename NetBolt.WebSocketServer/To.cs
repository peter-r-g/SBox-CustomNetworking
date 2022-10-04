using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NetBolt.WebSocket;

/// <summary>
/// Represents a set of target clients to send information to.
/// </summary>
public readonly struct To : IEnumerable<IWebSocketClient>
{
	/// <summary>
	/// A single client that is being targeted.
	/// </summary>
	private readonly IWebSocketClient? _singleClient;
	/// <summary>
	/// Multiple clients that are being targeted.
	/// </summary>
	private readonly IEnumerable<IWebSocketClient>? _multipleClients;
	/// <summary>
	/// The clients to ignore in <see cref="_multipleClients"/>.
	/// </summary>
	private readonly IEnumerable<IWebSocketClient>? _ignoredClients;

	private To( IWebSocketClient client )
	{
		_singleClient = client;
		_multipleClients = null;
		_ignoredClients = null;
	}

	private To( IEnumerable<IWebSocketClient> clients )
	{
		_singleClient = default;
		_multipleClients = clients;
		_ignoredClients = null;
	}

	private To( IEnumerable<IWebSocketClient> clients, IEnumerable<IWebSocketClient> ignoredClients )
	{
		_singleClient = default;
		_multipleClients = clients;
		_ignoredClients = ignoredClients;
	}

	/// <summary>
	/// Targets all currently connected clients.
	/// </summary>
	/// <param name="server">The server to fetch all clients from.</param>
	/// <returns>The target.</returns>
	public static To All( IWebSocketServer server )
	{
		return new To( server.Clients );
	}

	/// <summary>
	/// Targets a single client.
	/// </summary>
	/// <param name="client">The client to target.</param>
	/// <returns>The target.</returns>
	public static To Single( IWebSocketClient client )
	{
		return new To( client );
	}

	/// <summary>
	/// Targets multiple clients.
	/// </summary>
	/// <param name="clients"></param>
	/// <returns>The target.</returns>
	public static To Multiple( IEnumerable<IWebSocketClient> clients )
	{
		return new To( clients );
	}

	/// <summary>
	/// Targets <see cref="All"/> clients except for the provided clients.
	/// </summary>
	/// <param name="server">The server to fetch all clients from.</param>
	/// <param name="clientsToIgnore">The clients to ignore.</param>
	/// <returns>The target.</returns>
	public static To AllExcept( IWebSocketServer server, IEnumerable<IWebSocketClient> clientsToIgnore )
	{
		return new To( server.Clients, clientsToIgnore );
	}

	/// <summary>
	/// Targets <see cref="All"/> clients except for the provided clients.
	/// </summary>
	/// <param name="server">The server to fetch all clients from.</param>
	/// <param name="clientsToIgnore">The clients to ignore.</param>
	/// <returns>The target.</returns>
	public static To AllExcept( IWebSocketServer server, params IWebSocketClient[] clientsToIgnore )
	{
		return AllExcept( server, clientsToIgnore as IEnumerable<IWebSocketClient> );
	}

	public IEnumerator<IWebSocketClient> GetEnumerator()
	{
		if ( _singleClient is not null )
			yield return _singleClient;

		if ( _multipleClients is null )
			yield break;

		foreach ( var client in _multipleClients )
		{
			if ( _ignoredClients is null || !_ignoredClients.Contains( client ) )
				yield return client;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
