using CustomNetworking.Client.UI;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;
using Sandbox;

namespace CustomNetworking.Client;

public class MyGame : Game
{
	public new static MyGame Current => (Game.Current as MyGame)!;

	public readonly NetworkManager? NetworkManager;
	public readonly GameHud? GameHud;

	public MyGame()
	{
		if ( !IsClient )
			return;
		
		NetworkManager = new NetworkManager();
		NetworkManager.ClientConnected += OnClientConnected;
		NetworkManager.ClientDisconnected += OnClientDisconnected;
		NetworkManager.HandleMessage<SayMessage>( HandleSayMessage );
		GameHud = new GameHud();
	}
	
	private static void OnClientConnected( INetworkClient client )
	{
		ClientChatBox.AddInformation( $"{client.ClientId} has joined", $"avatar:{client.ClientId}" );
	}
	
	private static void OnClientDisconnected( INetworkClient client )
	{
		ClientChatBox.AddInformation( $"{client.ClientId} has left", $"avatar{client.ClientId}" );
	}

	private static void HandleSayMessage( NetworkMessage message )
	{
		if ( message is not SayMessage sayMessage )
			return;

		var clientName = sayMessage.Sender is null ? "Server" : sayMessage.Sender.ClientId.ToString();
		var avatar = sayMessage.Sender is null ? null : $"avatar:{sayMessage.Sender.ClientId}";
		ClientChatBox.AddChatEntry( clientName, sayMessage.Message, avatar );
	}

	[ConCmd.Client( "connect_to_server" )]
	public static void ConnectToServer()
	{
		_ = NetworkManager.Instance?.ConnectAsync();
	}

	[ConCmd.Client( "disconnect_from_server" )]
	public static void DisconnectFromServer()
	{
		NetworkManager.Instance?.Close();
	}
}
