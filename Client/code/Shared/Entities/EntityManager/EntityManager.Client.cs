#if CLIENT
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared;

public partial class EntityManager
{
	/// <summary>
	/// Deserializes an instance of <see cref="IEntity"/> and adds it to this <see cref="EntityManager"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="IEntity"/> from.</param>
	public void DeserializeAndAddEntity( NetworkReader reader )
	{
		var entity = reader.ReadEntity();
		_entities.Add( entity );
	}
}
#endif
