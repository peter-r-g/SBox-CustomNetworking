using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

public struct To : IEnumerable<INetworkClient>
{
	private INetworkClient? _singleClient;
	private IEnumerable<INetworkClient>? _multipleClients;
	private IEnumerable<INetworkClient>? _ignoredClients;
	
	public static To All => Multiple( NetworkManager.Clients.Values );

	public static To Single( INetworkClient client )
	{
		return new To {_singleClient = client};
	}

	public static To Multiple( IEnumerable<INetworkClient> clients )
	{
		return new To {_multipleClients = clients};
	}

	public static To AllExcept( IEnumerable<INetworkClient> clientsToIgnore )
	{
		return All with {_ignoredClients = clientsToIgnore};
	}

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
