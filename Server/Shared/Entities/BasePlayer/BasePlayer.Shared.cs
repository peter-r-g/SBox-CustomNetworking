using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;
using CustomNetworking.Shared.Utility;
using Sandbox;

namespace CustomNetworking.Shared.Entities;

public partial class BasePlayer : IEntity
{
	public event INetworkable<IEntity>.ChangedEventHandler? Changed;
	public NetworkedInt EntityId { get; }
	
	public BasePlayer( int entityId )
	{
		EntityId = entityId;
	}
	
	public virtual void Deserialize( NetworkReader reader )
	{
		_ = reader.ReadInt32();
	}

	public virtual void DeserializeChanges( NetworkReader reader )
	{
	}

	public virtual void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityId );
	}

	public virtual void SerializeChanges( NetworkWriter writer )
	{
	}
	
	public virtual void Delete()
	{
#if CLIENT
		PlayerPawn.Delete();
		Local.Client.Pawn = null;
#endif
	}

	public virtual void Update()
	{
	}
}
