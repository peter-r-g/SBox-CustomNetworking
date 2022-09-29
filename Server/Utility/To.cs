using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

/// <summary>
/// Represents a set of target clients to send information to.
/// </summary>
public readonly struct To : IEnumerable<INetworkClient>
{
	/// <summary>
	/// A single client that is being targeted.
	/// </summary>
	private readonly INetworkClient? _singleClient;
	/// <summary>
	/// Multiple clients that are being targeted.
	/// </summary>
	private readonly IEnumerable<INetworkClient>? _multipleClients;
	/// <summary>
	/// The clients to ignore in <see cref="_multipleClients"/>.
	/// </summary>
	private readonly IEnumerable<INetworkClient>? _ignoredClients;

	private To( INetworkClient client )
	{
		_singleClient = client;
		_multipleClients = null;
		_ignoredClients = null;
	}

	private To( IEnumerable<INetworkClient> clients )
	{
		_singleClient = null;
		_multipleClients = clients;
		_ignoredClients = null;
	}

	private To( IEnumerable<INetworkClient> clients, IEnumerable<INetworkClient> ignoredClients )
	{
		_singleClient = null;
		_multipleClients = clients;
		_ignoredClients = ignoredClients;
	}
	
	/// <summary>
	/// Targets all currently connected clients.
	/// </summary>
	public static To All => Multiple( NetworkServer.Instance.Clients.Values );

	/// <summary>
	/// Targets a single client.
	/// </summary>
	/// <param name="client">The client to target.</param>
	/// <returns>The target.</returns>
	public static To Single( INetworkClient client )
	{
		return new To( client );
	}

	/// <summary>
	/// Targets multiple clients.
	/// </summary>
	/// <param name="clients"></param>
	/// <returns>The target.</returns>
	public static To Multiple( IEnumerable<INetworkClient> clients )
	{
		return new To( clients );
	}

	/// <summary>
	/// Targets <see cref="All"/> clients except for the provided clients.
	/// </summary>
	/// <param name="clientsToIgnore">The clients to ignore.</param>
	/// <returns>The target.</returns>
	public static To AllExcept( IEnumerable<INetworkClient> clientsToIgnore )
	{
		return new To( NetworkServer.Instance.Clients.Values, clientsToIgnore );
	}

	/// <summary>
	/// Targets <see cref="All"/> clients except for the provided clients.
	/// </summary>
	/// <param name="clientsToIgnore">The clients to ignore.</param>
	/// <returns>The target.</returns>
	public static To AllExcept( params INetworkClient[] clientsToIgnore )
	{
		return AllExcept( clientsToIgnore as IEnumerable<INetworkClient> );
	}

	public IEnumerator<INetworkClient> GetEnumerator()
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
