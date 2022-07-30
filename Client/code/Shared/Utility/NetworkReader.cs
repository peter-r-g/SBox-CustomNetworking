using System;
using System.IO;
#if SERVER
using CustomNetworking.Server;
#endif
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.Utility;

public class NetworkReader : BinaryReader
{
	public NetworkReader( Stream input ) : base( input )
	{
	}

	public Guid ReadGuid()
	{
		return new Guid( ReadBytes( 16 ) );
	}

	public INetworkable ReadNetworkable()
	{
		var typeName = ReadString();
#if SERVER
		var type = TypeHelper.GetTypeByName( typeName );
		if ( type is null )
			throw new InvalidOperationException( $"Failed to read networkable (\"{typeName}\" does not exist in the current assembly)." );

		if ( type.IsGenericType )
		{
			var genericCount = ReadInt32();
			var genericTypes = new Type[genericCount];
			for ( var i = 0; i < genericCount; i++ )
			{
				var genericTypeName = ReadString();
				var genericType = TypeHelper.GetTypeByName( genericTypeName );

				genericTypes[i] = genericType ?? throw new InvalidOperationException( $"Failed to read networkable (Generic argument \"{genericTypeName}\" does not exist in the current assembly)" );
			}

			type = type.MakeGenericType( genericTypes );
		}
		
		var networkable = (INetworkable?)Activator.CreateInstance( type );
		if ( networkable is null )
			throw new InvalidOperationException( "Failed to read networkable (instance creation failed)" );
#endif
#if CLIENT
		var type = TypeLibrary.GetTypeByName( typeName );
		if ( type is null )
			throw new InvalidOperationException( $"Failed to read networkable (\"{typeName}\" does not exist in the {nameof(TypeLibrary)})." );
		
		if ( type.IsGenericType )
		{
			var genericCount = ReadInt32();
			var genericTypes = new Type[genericCount];
			for ( var i = 0; i < genericCount; i++ )
			{
				var genericTypeName = ReadString();
				var genericType = TypeLibrary.GetTypeByName( genericTypeName );
				if ( genericType is null )
					throw new InvalidOperationException( $"Failed to read networkable (Generic argument \"{genericTypeName}\" does not exist in the current assembly)" );

				genericTypes[i] = genericType;
			}

			type = type.MakeGenericType( genericTypes );
		}
		
		var networkable = TypeLibrary.Create<INetworkable>( type );
#endif
		
		networkable.Deserialize( this );
		return networkable;
	}

	public T ReadNetworkable<T>() where T : INetworkable
	{
		if ( typeof(T).IsAssignableTo( typeof(IEntity) ) )
			return ReadEntity<T>();
		
		var networkable = ReadNetworkable();
		if ( networkable is not T outputNetworkable )
			throw new InvalidOperationException( $"Failed to read networkable ({networkable.GetType()} is not assignable to {typeof(T)})" );

		return outputNetworkable;
	}

	public IEntity ReadEntity()
	{
		var typeName = ReadString();
		var entityId = ReadInt32();
		BaseStream.Position -= sizeof(int);
#if SERVER
		var type = TypeHelper.GetTypeByName( typeName );
		if ( type is null )
			throw new InvalidOperationException( $"Failed to read entity (\"{typeName}\" does not exist in the current assembly)." );
		
		var entity = (IEntity?)Activator.CreateInstance( type, entityId );
		if ( entity is null )
			throw new InvalidOperationException( "Failed to read entity (instance creation failed)" );
#endif
#if CLIENT
		var type = TypeLibrary.GetTypeByName( typeName );
		var entity = TypeLibrary.Create<IEntity>( type, new object[] {entityId} );
#endif
		
		entity.Deserialize( this );
		return entity;
	}

	public T ReadEntity<T>()
	{
		var entity = ReadEntity();
		if ( entity is not T outputEntity )
			throw new InvalidOperationException( $"Failed to read entity ({entity.GetType()} is not assignable to {typeof(T)})" );

		return outputEntity;
	}

	public void ReadNetworkableChanges( INetworkable networkable )
	{
		networkable.DeserializeChanges( this );
	}
}
