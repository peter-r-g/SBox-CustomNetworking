using System.Collections.Generic;
using System.Numerics;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;

namespace CustomNetworking.Server;

public static class Input
{
	private static readonly Dictionary<INetworkClient, (ClientInput, ClientInput)> InputButtons = new();

	public static bool IsUsingController( INetworkClient client )
	{
		return InputButtons.TryGetValue( client, out var inputs ) && inputs.Item1.UsingController;
	}

	public static Quaternion GetRotation( INetworkClient client )
	{
		return InputButtons.TryGetValue( client, out var inputs ) ? inputs.Item1.Rotation : Quaternion.Identity;
	}

	public static int GetMouseWheel( INetworkClient client )
	{
		return InputButtons.TryGetValue( client, out var inputs ) ? inputs.Item1.MouseWheel : 0;
	}

	public static bool Down( INetworkClient client, InputButton button )
	{
		if ( !InputButtons.TryGetValue( client, out var buttons ) )
			return false;

		return (buttons.Item1.Buttons & button) == button;
	}

	public static bool Pressed( INetworkClient client, InputButton button )
	{
		if ( !InputButtons.TryGetValue( client, out var buttons ) )
			return false;

		return (buttons.Item1.Buttons & button) == button && (buttons.Item2.Buttons & button) != button;
	}

	public static bool Released( INetworkClient client, InputButton button )
	{
		if ( !InputButtons.TryGetValue( client, out var buttons ) )
			return false;

		return (buttons.Item1.Buttons & button) != button && (buttons.Item2.Buttons & button) == button;
	}

	internal static void OnClientConnected( INetworkClient client )
	{
		InputButtons.Add( client, (ClientInput.Default, ClientInput.Default) );
	}

	internal static void OnClientDisconnected( INetworkClient client )
	{
		InputButtons.Remove( client );
	}
	
	internal static void HandleClientInputMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not ClientInputMessage clientInputMessage )
			return;

		var input = new ClientInput( clientInputMessage.UsingController, clientInputMessage.Rotation,
			clientInputMessage.MouseWheel, clientInputMessage.Buttons );
		InputButtons[client] = (input, InputButtons[client].Item1);
	}
}
