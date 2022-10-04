#if CLIENT
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Entities;

public partial class EntityManager
{
	/// <summary>
	/// Creates and adds a new <see cref="IEntity"/> to this <see cref="EntityManager"/>.
	/// </summary>
	/// <param name="entityClass">The class name of the <see cref="IEntity"/> to create.</param>
	/// <param name="entityId">The <see cref="IEntity"/>.<see cref="IEntity.EntityId"/> of the created <see cref="IEntity"/>.</param>
	public void Create( string entityClass, int entityId )
	{
		var entityType = TypeHelper.GetTypeByName( entityClass );
		if ( entityType is null || !TypeHelper.IsClass( entityType ) || !entityType.IsAssignableTo( typeof(IEntity) ) )
		{
			Logging.Error( $"Failed to create entity (type is not a class that implements {nameof(IEntity)})." );
			return;
		}

		CreateInternal<IEntity>( entityId, entityType );
	}
	
	/// <summary>
	/// Deserializes an instance of <see cref="IEntity"/> and adds it to this <see cref="EntityManager"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="IEntity"/> from.</param>
	public void DeserializeAndAddEntity( NetworkReader reader )
	{
		var entity = reader.ReadEntity();
		_entities.Add( entity.EntityId, entity );
	}
}
#endif
