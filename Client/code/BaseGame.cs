using CustomNetworking.Client.UI;
using CustomNetworking.Shared;
using Sandbox;
using Logging = CustomNetworking.Shared.Utility.Logging;

namespace CustomNetworking.Client;

public class BaseGame : Game
{
	public new static BaseGame Current => (Sandbox.Game.Current as BaseGame)!;

	public readonly NetworkManager? NetworkManager;
	public readonly GameHud? GameHud;

	public BaseGame()
	{
		if ( !IsClient )
			return;

		NetworkManager = new NetworkManager();
		NetworkManager.ConnectedToServer += OnConnectedToServer;
		NetworkManager.DisconnectedFromServer += OnDisconnectedFromServer;
		NetworkManager.ClientConnected += OnClientConnected;
		NetworkManager.ClientDisconnected += OnClientDisconnected;
		GameHud = new GameHud();
	}

	[Event.Tick.Client]
	protected virtual void Update()
	{
		if ( NetworkManager is null || !NetworkManager.Connected )
			return;

		NetworkManager.Update();
	}
	
	protected virtual void OnConnectedToServer()
	{
		Logging.Info( "Connected" );
	}
	
	protected virtual void OnDisconnectedFromServer()
	{
		Logging.Info( "Disconnected" );
	}
	
	protected virtual void OnClientConnected( INetworkClient client )
	{
		ClientChatBox.AddInformation( $"{client.ClientId} has joined", $"avatar:{client.ClientId}" );
	}
	
	protected virtual void OnClientDisconnected( INetworkClient client )
	{
		ClientChatBox.AddInformation( $"{client.ClientId} has left", $"avatar{client.ClientId}" );
	}

	[ConCmd.Client( "connect_to_server" )]
	public static void ConnectToServer()
	{
		_ = NetworkManager.Instance?.ConnectAsync( "127.0.0.1", SharedConstants.Port, false );
	}

	[ConCmd.Client( "disconnect_from_server" )]
	public static void DisconnectFromServer()
	{
		NetworkManager.Instance?.Close();
	}
}
