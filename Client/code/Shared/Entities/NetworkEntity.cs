#if CLIENT
using Sandbox;
#endif

namespace CustomNetworking.Shared.Entities;

public class NetworkEntity
#if SERVER
	: IEntity
#endif
#if CLIENT
	: Entity, IEntity
#endif
{
	public int EntityId { get; }

	public NetworkEntity( int entityId )
	{
		EntityId = entityId;
	}
}
