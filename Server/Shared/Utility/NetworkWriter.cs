using System.IO;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.Utility;

public class NetworkWriter : BinaryWriter
{
	public NetworkWriter( Stream output ) : base( output )
	{
	}

	public void WriteNetworkable( INetworkable networkable )
	{
		Write( networkable.GetType().Name );
		networkable.Serialize( this );
	}

	public void WriteNetworkableChanges( INetworkable networkable )
	{
		networkable.SerializeChanges( this );
	}
}
