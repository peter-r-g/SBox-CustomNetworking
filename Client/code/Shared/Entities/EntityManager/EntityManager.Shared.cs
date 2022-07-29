using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

public partial class EntityManager
{
	public readonly List<IEntity> Entities = new();
	private int _nextEntityId;

	public INetworkable.ChangedEventHandler? EntityChanged;

	private T CreateInternal<T>( int entityId, Type? entityType = null ) where T : IEntity
	{
#if SERVER
		var entity = (T?)Activator.CreateInstance( entityType ?? typeof(T), entityId );
#endif
#if CLIENT
		var entity = TypeLibrary.Create<T>( entityType ?? typeof(T), new object[] {entityId} );
#endif
		if ( entity is null )
			throw new Exception( $"Failed to create instance of {entityType ?? typeof(T)}" );
		
		Entities.Add( entity );
		entity.Changed += EntityOnChanged;
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

	public IEntity? GetEntityById( int entityId )
	{
		foreach ( var entity in Entities )
		{
			if ( entity.EntityId == entityId )
				return entity;	
		}

		return null;
	}
	
	private void EntityOnChanged( INetworkable entity )
	{
		EntityChanged?.Invoke( entity );
	}
}
