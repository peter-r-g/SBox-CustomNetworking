using System.Text;
using NetBolt.Shared.Entities;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace NetBolt.Client;

[UseTemplate]
public class GameHud : RootPanel
{
	public string ClientCount => $"{NetworkManager.Instance?.Clients.Count} clients connected";
	public string NetworkedEntityCount => $"{IEntity.All.Entities.Count} networked entities";

#if DEBUG
	public string MessagesReceived => $"{NetworkManager.Instance?.MessagesReceived} messages received";
	public string MessagesSent => $"{NetworkManager.Instance?.MessagesSent} messages sent";

	public string MessageTypesReceived
	{
		get
		{
			if ( NetworkManager.Instance is null )
				return string.Empty;

			var sb = new StringBuilder();
			sb.Append( "Received Types:\n" );

			foreach ( var pair in NetworkManager.Instance.MessageTypesReceived )
			{
				sb.Append( pair.Key.Name );
				sb.Append( ": " );
				sb.Append( pair.Value );
				sb.Append( '\n' );
			}

			return sb.ToString();
		}
	}

	public GameHud()
	{
		var messagesReceivedLabel = Add.Label( MessagesReceived, "debugLabel netReceivedNum" );
		var messagesSentLabel = Add.Label( MessagesSent, "debugLabel netSentNum" );
		var messageTypesReceivedLabel = Add.Label( MessageTypesReceived, "debugLabel netTypeReceivedNum" );
		messagesReceivedLabel.Bind( "text", () => MessagesReceived );
		messagesSentLabel.Bind( "text", () => MessagesSent );
		messageTypesReceivedLabel.Bind( "text", () => MessageTypesReceived );
	}
#endif
}
