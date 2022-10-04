using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Messages;

/// <summary>
/// A <see cref="NetworkMessage"/> that notifies clients that the server is shutting down.
/// </summary>
public sealed class ShutdownMessage : NetworkMessage
{
	public override void Deserialize( NetworkReader reader )
	{
	}

	public override void Serialize( NetworkWriter writer )
	{
	}
}
