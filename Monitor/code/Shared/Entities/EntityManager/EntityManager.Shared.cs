using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared;

/// <summary>
/// Container for a collection of entities.
/// </summary>
public partial class EntityManager
{
	/// <summary>
	/// A read only list of all <see cref="IEntity"/>s that are in this <see cref="EntityManager"/>.
	/// </summary>
	public IReadOnlyList<IEntity> Entities => _entities;
	
	/// <summary>
	/// Called when an <see cref="IEntity"/> inside this <see cref="EntityManager"/> has changed.
	/// </summary>
	public INetworkable.ChangedEventHandler? EntityChanged;
	
	private readonly List<IEntity> _entities = new();
	private int _nextEntityId;

	/// <summary>
	/// Creates and adds a new <see cref="IEntity"/> to this <see cref="EntityManager"/>.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="IEntity"/> to create.</typeparam>
	/// <returns>The created <see cref="IEntity"/> as <see cref="T"/>.</returns>
	public T Create<T>() where T : class, IEntity
	{
		var entity = CreateInternal<T>( _nextEntityId );
		_nextEntityId++;
		return entity;
	}

	/// <summary>
	/// Creates and adds a new <see cref="IEntity"/> to this <see cref="EntityManager"/>.
	/// /// </summary>
	/// <param name="entityType">The type of <see cref="IEntity"/> to create.</param>
	/// <returns>The created <see cref="IEntity"/>.</returns>
	/// <exception cref="Exception">Thrown when <see cref="entityType"/> is not a class or does not implement <see cref="IEntity"/>.</exception>
	public IEntity Create( Type entityType )
	{
		if ( !entityType.IsClass || !entityType.IsAssignableTo( typeof(IEntity) ) )
			throw new Exception( $"Failed to create entity (type is not a class that implements {nameof(IEntity)})." );

		var entity = CreateInternal<IEntity>( _nextEntityId, entityType );
		_nextEntityId++;
		return entity;
	}

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	/// <remarks>You should not use the <see cref="entity"/> after calling this.</remarks>
	/// <param name="entity">The entity to delete.</param>
	public void DeleteEntity( IEntity entity )
	{
		entity.Changed -= EntityOnChanged;
		entity.Delete();
	}

	/// <summary>
	/// Deletes an entity with the given entity identifier.
	/// </summary>
	/// <param name="entityId">The entity identifier to lookup and delete.</param>
	/// <exception cref="InvalidOperationException">Thrown when no entity with the given <see cref="entityId"/> is found.</exception>
	public void DeleteEntity( int entityId )
	{
		var entity = GetEntityById( entityId );
		if ( entity is null )
			throw new InvalidOperationException( $"Failed to delete entity (No entity with the ID \"{entityId}\" exists)." );

		DeleteEntity( entity );
	}

	/// <summary>
	/// Deletes all entities in this <see cref="EntityManager"/>.
	/// </summary>
	public void DeleteAllEntities()
	{
		foreach ( var entity in _entities )
			DeleteEntity( entity );
	}

	/// <summary>
	/// Gets an <see cref="IEntity"/> in this <see cref="EntityManager"/>.
	/// </summary>
	/// <param name="entityId">The ID of the <see cref="IEntity"/> to get.</param>
	/// <returns>The <see cref="IEntity"/> that was found. Null if no <see cref="IEntity"/> was found.</returns>
	public IEntity? GetEntityById( int entityId )
	{
		foreach ( var entity in _entities )
		{
			if ( entity.EntityId == entityId )
				return entity;	
		}

		return null;
	}
	
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
		
		_entities.Add( entity );
		entity.Changed += EntityOnChanged;
		return entity;
	}
	
	private void EntityOnChanged( INetworkable entity )
	{
		EntityChanged?.Invoke( entity );
	}
}