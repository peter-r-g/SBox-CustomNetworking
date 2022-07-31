using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class ShutdownMessage : NetworkMessage
{
	public override void Deserialize( NetworkReader reader )
	{
	}

	public override void Serialize( NetworkWriter writer )
	{
	}
}
