using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Server;

public class EntityManager
{
	public static readonly List<IEntity> All = new();
	private static int _nextEntityId;

	private T CreateInternal<T>( int entityId, Type? entityType = null ) where T : IEntity
	{
		var entity = (T?)Activator.CreateInstance( entityType ?? typeof(T), entityId );
		if ( entity is null )
			throw new Exception( $"Failed to create instance of {entityType ?? typeof(T)}" );
		
		All.Add( entity );
		return entity;
	}

	public T Create<T>() where T : class, IEntity
	{
		var entity = CreateInternal<T>( _nextEntityId );
		_nextEntityId++;
		return entity;
	}

	public IEntity Create( Type entityType )
	{
		if ( !entityType.IsClass || !entityType.IsAssignableTo( typeof(IEntity) ) )
			throw new Exception( $"Failed to create entity (type is not a class that implements {nameof(IEntity)})." );

		var entity = CreateInternal<IEntity>( _nextEntityId, entityType );
		_nextEntityId++;
		return entity;
	}

#if CLIENT
	public IEntity Create( string entityClass, int entityId )
	{
		var entityType = TypeLibrary.GetTypeByName( entityClass );
		if ( entityType is null )
			throw new Exception( $"Failed to create entity (class \"{entityClass}\" was not found)." );
		
		return CreateInternal<IEntity>( entityId, entityType );
	}
#endif
}
