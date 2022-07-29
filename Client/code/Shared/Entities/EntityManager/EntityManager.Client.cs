#if CLIENT
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared;

public partial class EntityManager
{
	public void DeserializeAndAddEntity( NetworkReader reader )
	{
		var entity = reader.ReadEntity();
		Entities.Add( entity );
	}
}
#endif
