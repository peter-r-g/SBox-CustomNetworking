using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Entities;

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
	/// The event handler for <see cref="EntityManager"/>.<see cref="EntityManager.EntityCreated"/>.
	/// </summary>
	public delegate void CreatedEventHandler( IEntity entity );
	/// <summary>
	/// Called when an <see cref="IEntity"/> has been created in this <see cref="EntityManager"/>.
	/// </summary>
	public CreatedEventHandler? EntityCreated;

	/// <summary>
	/// The event handler for <see cref="EntityManager"/>.<see cref="EntityManager.EntityDeleted"/>.
	/// </summary>
	public delegate void DeletedEventHandler( IEntity entity );
	/// <summary>
	/// Called when an <see cref="IEntity"/> has been deleted in the <see cref="EntityManager"/>.
	/// </summary>
	public DeletedEventHandler? EntityDeleted;

	/// <summary>
	/// The event handler for <see cref="EntityManager"/>.<see cref="EntityManager.EntityChanged"/>.
	/// </summary>
	public delegate void ChangedEventHandler( IEntity entity );
	/// <summary>
	/// Called when an <see cref="IEntity"/> inside this <see cref="EntityManager"/> has changed.
	/// </summary>
	public ChangedEventHandler? EntityChanged;
	
	private readonly List<IEntity> _entities = new();
	private int _nextEntityId;

	/// <summary>
	/// Creates and adds a new <see cref="IEntity"/> to this <see cref="EntityManager"/>.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="IEntity"/> to create.</typeparam>
	/// <returns>The created <see cref="IEntity"/> as <see cref="T"/>.</returns>
	public T Create<T>() where T : class, IEntity
	{
		return CreateInternal<T>( _nextEntityId++ );
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
		{
			Logging.Error( $"Failed to create entity ({nameof(entityType)} is not a class that implementes {nameof(IEntity)}).", new InvalidOperationException() );
			return null!;
		}

		return CreateInternal<IEntity>( _nextEntityId++, entityType );
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
		EntityDeleted?.Invoke( entity );
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
		{
			Logging.Error( $"Failed to delete entity (No entity with the ID \"{entityId}\" exists).", new InvalidOperationException() );
			return;
		}

		DeleteEntity( entity );
		_entities.Remove( entity );
	}

	/// <summary>
	/// Deletes all entities in this <see cref="EntityManager"/>.
	/// </summary>
	public void DeleteAllEntities()
	{
		foreach ( var entity in _entities )
			DeleteEntity( entity );
		
		_entities.Clear();
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
		var entity = TypeHelper.Create<T>( entityType ?? typeof(T), entityId );
		if ( entity is null )
		{
			Logging.Error( $"Failed to create instance of {entityType ?? typeof(T)}", new InvalidOperationException() );
			return default!;
		}
		
		_entities.Add( entity );
		entity.Changed += EntityOnChanged;
		EntityCreated?.Invoke( entity );
		return entity;
	}
	
	private void EntityOnChanged( IEntity oldEntity, IEntity newEntity )
	{
		EntityChanged?.Invoke( newEntity );
	}
}
