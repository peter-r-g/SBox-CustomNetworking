using Sandbox.UI;

namespace CustomNetworking.Client;

[UseTemplate]
public class GameHud : RootPanel
{
	public string ClientCount => $"{NetworkManager.Instance?.Clients.Count} clients connected";
}
