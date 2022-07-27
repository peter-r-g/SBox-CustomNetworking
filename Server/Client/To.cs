using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

public struct To : IEnumerable<INetworkClient>
{
	public INetworkClient? SingleClient;
	public IEnumerable<INetworkClient>? MultipleClients;
	public IEnumerable<INetworkClient>? IgnoredClients;
	
	public static To All => Multiple( NetworkManager.Clients.Values );

	public static To Single( INetworkClient client )
	{
		return new To {SingleClient = client};
	}

	public static To Multiple( IEnumerable<INetworkClient> clients )
	{
		return new To {MultipleClients = clients};
	}

	public static To AllExcept( IEnumerable<INetworkClient> clientsToIgnore )
	{
		return All with {IgnoredClients = clientsToIgnore};
	}

	public static To AllExcept( params INetworkClient[] clientsToIgnore )
	{
		return AllExcept( clientsToIgnore as IEnumerable<INetworkClient> );
	}

	public IEnumerator<INetworkClient> GetEnumerator()
	{
		if ( SingleClient is not null )
			yield return SingleClient;

		if ( MultipleClients is null )
			yield break;

		foreach ( var client in MultipleClients )
		{
			if ( IgnoredClients is null || !IgnoredClients.Contains( client ) )
				yield return client;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
