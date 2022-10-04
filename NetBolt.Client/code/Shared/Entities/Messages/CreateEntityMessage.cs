using NetBolt.Shared.Entities;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A server to client <see cref="NetworkMessage"/> that contains information to create a new <see cref="IEntity"/>.
/// </summary>
public sealed class CreateEntityMessage : NetworkMessage
{
	/// <summary>
	/// The class name of the <see cref="IEntity"/>.
	/// </summary>
	public string EntityClass { get; private set; } = "";
	/// <summary>
	/// The unique identifier the <see cref="IEntity"/> has.
	/// </summary>
	public int EntityId { get; private set; }

#if SERVER
	public CreateEntityMessage( IEntity entity )
	{
		EntityClass = entity.GetType().Name;
		EntityId = entity.EntityId;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		EntityClass = reader.ReadString();
		EntityId = reader.ReadInt32();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityClass );
		writer.Write( EntityId );
	}
}
