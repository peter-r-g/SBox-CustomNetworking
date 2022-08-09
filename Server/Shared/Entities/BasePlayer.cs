using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;
using CustomNetworking.Shared.Utility;
#if CLIENT
using CustomNetworking.Client;
using Sandbox;
#endif

namespace CustomNetworking.Shared.Entities;

public partial class BasePlayer : IEntity
{
	public event INetworkable<IEntity>.ChangedEventHandler? Changed;
	public NetworkedInt EntityId { get; }
	
#if CLIENT
	protected readonly TestPlayer PlayerPawn;
#endif
	
	public BasePlayer( int entityId )
	{
		EntityId = entityId;
		
#if CLIENT
		PlayerPawn = new TestPlayer();
		Local.Client.Pawn = PlayerPawn;
#endif
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
