using System.Numerics;
#if SERVER
using CustomNetworking.Server;
#endif
using CustomNetworking.Shared.Utility;
#if CLIENT
using Sandbox;
#endif

namespace CustomNetworking.Shared.Messages;

public sealed class ClientInputMessage : NetworkMessage
{
	public bool UsingController { get; private set; }
	public Quaternion Rotation { get; private set; }
	public int MouseWheel { get; private set; }
	public InputButton Buttons { get; private set; }
	
#if CLIENT
	public ClientInputMessage( bool usingController, Rotation rotation, int mouseWheel, InputButton buttons )
	{
		UsingController = usingController;
		Rotation = new Quaternion( rotation.x, rotation.y, rotation.z, rotation.w );
		MouseWheel = mouseWheel;
		Buttons = buttons;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
#if SERVER
		UsingController = reader.ReadBoolean();
		Rotation = reader.ReadQuaternion();
		MouseWheel = reader.ReadInt32();
		Buttons = (InputButton)reader.ReadUInt64();
#endif
	}

	public override void Serialize( NetworkWriter writer )
	{
#if CLIENT
		writer.Write( UsingController );
		writer.Write( Rotation );
		writer.Write( MouseWheel );
		writer.Write( (ulong)Buttons );
#endif
	}
}
