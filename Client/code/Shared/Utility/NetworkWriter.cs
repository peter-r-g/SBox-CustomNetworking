using System;
using System.IO;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.Utility;

public class NetworkWriter : BinaryWriter
{
	public NetworkWriter( Stream output ) : base( output )
	{
	}

	public void Write( Guid guid )
	{
		Write( guid.ToByteArray() );
	}

	// TODO: When writing an entity, if it is referenced under and entity just send the entity ID rather than the whole entity.
	public void WriteNetworkable( INetworkable networkable )
	{
		var networkableType = networkable.GetType();
		Write( networkableType.Name );
		if ( networkableType.IsGenericType )
		{
			var genericArguments = networkableType.GetGenericArguments();
			Write( genericArguments.Length );
			foreach ( var type in genericArguments )
				Write( type.Name );
		}
		
		networkable.Serialize( this );
	}

	public void WriteNetworkableChanges( INetworkable networkable )
	{
		networkable.SerializeChanges( this );
	}
}
