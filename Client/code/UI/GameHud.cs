using Sandbox.UI;
using Sandbox.UI.Construct;

namespace CustomNetworking.Client;

[UseTemplate]
public class GameHud : RootPanel
{
	public string ClientCount => $"{NetworkManager.Instance?.Clients.Count} clients connected";
	public string NetworkedEntityCount => $"{MyGame.Current.EntityManager?.Entities.Count} networked entities";
	
#if DEBUG
	public string MessagesReceived => $"{NetworkManager.Instance?.MessagesReceived} messages received";
	public string MessagesSent => $"{NetworkManager.Instance?.MessagesSent} messages sent";

	public GameHud()
	{
		var messagesReceivedLabel = Add.Label( MessagesReceived, "debugLabel netReceivedNum" );
		var messagesSentLabel = Add.Label( MessagesSent, "debugLabel netSentNum" );
		messagesReceivedLabel.Bind( "text", () => MessagesReceived );
		messagesSentLabel.Bind( "text", () => MessagesSent );
	}
#endif
}
