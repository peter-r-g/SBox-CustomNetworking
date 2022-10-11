using System;
#if SERVER
using System.Collections.Generic;
using System.Linq;
using NetBolt.Shared.Messages;
#endif
using NetBolt.Shared.Networkables;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// Base class for sending any information between a client and a server.
/// </summary>
public abstract class NetworkMessage : INetworkable
{
	public bool Changed() => false;

	public abstract void Deserialize( NetworkReader reader );
	public void DeserializeChanges( NetworkReader reader )
	{
		Deserialize( reader );
	}

	public abstract void Serialize( NetworkWriter writer );
	public void SerializeChanges( NetworkWriter writer )
	{
		Serialize( writer );
	}

	public static NetworkMessage DeserializeMessage( NetworkReader reader )
	{
		return reader.ReadNetworkable<NetworkMessage>();
	}
}
