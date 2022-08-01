using System.Numerics;

namespace CustomNetworking.Server;

internal sealed class ClientInput
{
	internal static ClientInput Default => new( false, Quaternion.Identity, 0, 0 );
	
	internal bool UsingController { get; }
	internal Quaternion Rotation { get; }
	internal int MouseWheel { get; }
	internal InputButton Buttons { get; }

	internal ClientInput( bool usingController, Quaternion rotation, int mouseWheel, InputButton buttons )
	{
		UsingController = usingController;
		Rotation = rotation;
		MouseWheel = mouseWheel;
		Buttons = buttons;
	}
}
