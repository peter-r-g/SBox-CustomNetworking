using System;
using System.IO;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.Utility;

/// <summary>
/// Writes any data relating to networking and primitive types.
/// </summary>
public class NetworkWriter : BinaryWriter
{
	public NetworkWriter( Stream output ) : base( output )
	{
	}

	/// <summary>
	/// Writes a 16 byte Globally Unique Identifier (<see cref="Guid"/>).
	/// </summary>
	/// <param name="guid">The instance of <see cref="Guid"/> to write.</param>
	public void Write( Guid guid )
	{
		Write( guid.ToByteArray() );
	}

	/// <summary>
	/// Writes a 3 float <see cref="System.Numerics.Vector3"/>.
	/// </summary>
	/// <param name="vector3">The instance of <see cref="System.Numerics.Vector3"/> to write.</param>
	public void Write( System.Numerics.Vector3 vector3 )
	{
		Write( vector3.X );
		Write( vector3.Y );
		Write( vector3.Y );
	}

	// TODO: When writing an entity, if it is referenced under an entity just send the entity ID rather than the whole entity.
	/// <summary>
	/// Writes an instance of <see cref="INetworkable"/>.
	/// </summary>
	/// <param name="networkable">The instance of <see cref="INetworkable"/> to write.</param>
	public void WriteNetworkable<T>( INetworkable<T> networkable )
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

	/// <summary>
	/// Writes the changes of an <see cref="INetworkable"/>.
	/// </summary>
	/// <param name="networkable">The instance of <see cref="INetworkable"/> to write changes.</param>
	public void WriteNetworkableChanges<T>( INetworkable<T> networkable )
	{
		networkable.SerializeChanges( this );
	}
}
