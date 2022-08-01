using System;
using CustomNetworking.Shared.Messages;
using Sandbox;

namespace CustomNetworking.Client;

internal static class InputHelper
{
	private static bool _lastUsingController;
	private static Rotation _lastRotation;
	private static int _lastMouseWheel;
	private static InputButton _lastButtons;
	private static InputButton _buttons;
	
	private static InputButton GetButtons()
	{
		var buttons = (InputButton)0;
		foreach ( var button in Enum.GetValues( typeof(InputButton) ) )
		{
			var buttonType = (InputButton)button;
			if ( Input.Down( buttonType ) )
				buttons |= buttonType;
		}
		return buttons;
	}

	internal static void SendInputToServer()
	{
		var isUsingController = Input.UsingController;
		var rotation = Input.Rotation;
		var mouseWheel = Input.MouseWheel;
		var inputButtons = GetButtons();
		if ( _lastUsingController == isUsingController && _lastRotation == rotation && _lastMouseWheel == mouseWheel &&
		     _lastButtons == inputButtons )
			return;

		_lastUsingController = isUsingController;
		_lastRotation = rotation;
		_lastMouseWheel = mouseWheel;
		_lastButtons = _buttons;
		_buttons = inputButtons;
		NetworkManager.Instance?.SendToServer( new ClientInputMessage( isUsingController, rotation, mouseWheel,
			inputButtons ) );
	}
}
