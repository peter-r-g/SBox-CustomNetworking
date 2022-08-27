using System;
using System.IO;
using System.Numerics;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.Utility;

/// <summary>
/// Reads any data relating to networking and primitive types.
/// </summary>
public class NetworkReader : BinaryReader
{
	public NetworkReader( Stream input ) : base( input )
	{
	}

	/// <summary>
	/// Reads a 16 byte Globally Unique Identifier (<see cref="Guid"/>).
	/// </summary>
	/// <returns>The parsed <see cref="Guid"/>.</returns>
	public Guid ReadGuid()
	{
		return new Guid( ReadBytes( 16 ) );
	}

	/// <summary>
	/// Reads a 4 float <see cref="Quaternion"/>.
	/// </summary>
	/// <returns>The parsed <see cref="Quaternion"/>.</returns>
	public Quaternion ReadQuaternion()
	{
		return new Quaternion( ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle() );
	}

	/// <summary>
	/// Reads a 3 float <see cref="System.Numerics.Vector3"/>.
	/// </summary>
	/// <returns>The parsed <see cref="System.Numerics.Vector3"/>.</returns>
	public System.Numerics.Vector3 ReadVector3()
	{
		return new Vector3( ReadSingle(), ReadSingle(), ReadSingle() );
	}

	/// <summary>
	/// Reads an instance of <see cref="INetworkable"/>.
	/// </summary>
	/// <returns>The parsed <see cref="INetworkable"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when reading the <see cref="INetworkable"/> has failed.</exception>
	public INetworkable ReadNetworkable()
	{
		var typeName = ReadString();
		var type = TypeHelper.GetTypeByName( typeName );
		if ( type is null )
		{
			Logging.Error( $"Failed to read networkable (\"{typeName}\" does not exist)", new InvalidOperationException() );
			return null!;
		}

		if ( type.IsGenericType )
		{
			var genericCount = ReadInt32();
			var genericTypes = new Type[genericCount];
			for ( var i = 0; i < genericCount; i++ )
			{
				var genericTypeName = ReadString();
				var genericType = TypeHelper.GetTypeByName( genericTypeName );
				if ( genericType is null )
				{
					Logging.Error( $"Failed to read networkable (Generic argument \"{genericTypeName}\" does not exist).", new InvalidOperationException() );
					return null!;
				}

				genericTypes[i] = genericType;
			}

			type = type.MakeGenericType( genericTypes );
		}
		
		var networkable = TypeHelper.Create<INetworkable>( type );
		if ( networkable is null )
		{
			Logging.Error( "Failed to read networkable (instance creation failed).", new InvalidOperationException() );
			return null!;
		}
		
		networkable.Deserialize( this );
		return networkable;
	}

	/// <summary>
	/// Reads an instance of <see cref="INetworkable"/> and casts it to <see cref="T"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="INetworkable"/> type to cast into.</typeparam>
	/// <returns>The parsed <see cref="INetworkable"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when reading the <see cref="INetworkable"/> has failed.</exception>
	public T ReadNetworkable<T>() where T : INetworkable
	{
		if ( typeof(T).IsAssignableTo( typeof(IEntity) ) )
			return ReadEntity<T>();
		
		var networkable = ReadNetworkable();
		if ( networkable is not T outputNetworkable )
		{
			Logging.Error( $"Failed to read networkable ({networkable.GetType()} is not assignable to {typeof(T)}).", new InvalidOperationException() );
			return default!;
		}

		return outputNetworkable;
	}
	
	/// <summary>
	/// Reads all changes relating to an <see cref="INetworkable"/> instance.
	/// </summary>
	/// <param name="networkable">The <see cref="INetworkable"/> to deserialize the changes into.</param>
	public void ReadNetworkableChanges( INetworkable networkable )
	{
		networkable.DeserializeChanges( this );
	}

	// TODO: When reading an entity. Try to only read the entity ID. If there's more info after that then read a whole entity.
	/// <summary>
	/// Reads an instance of <see cref="IEntity"/>.
	/// </summary>
	/// <returns>The parsed <see cref="IEntity"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when reading the <see cref="IEntity"/> has failed.</exception>
	public IEntity ReadEntity()
	{
		var entityId = ReadInt32();
		var typeName = ReadString();
		
		var type = TypeHelper.GetTypeByName( typeName );
		if ( type is null )
		{
			Logging.Error( $"Failed to read entity (\"{typeName}\" does not exist)", new InvalidOperationException() );
			return null!;
		}

		var entity = TypeHelper.Create<IEntity?>( type, entityId );
		if ( entity is null )
		{
			Logging.Error( "Failed to read entity (instance creation failed).", new InvalidOperationException() );
			return null!;
		}
		
		entity.Deserialize( this );
		return entity;
	}
	
	/// <summary>
	/// Reads an instance of <see cref="IEntity"/> and casts it to <see cref="T"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="IEntity"/> type to cast into.</typeparam>
	/// <returns>The parsed <see cref="IEntity"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when reading the <see cref="IEntity"/> has failed.</exception>
	public T ReadEntity<T>()
	{
		var entity = ReadEntity();
		if ( entity is not T outputEntity )
		{
			Logging.Error( $"Failed to read entity ({entity.GetType()} is not assignable to {typeof(T)})", new InvalidOperationException() );
			return default!;
		}

		return outputEntity;
	}
}
