using CustomNetworking.Client.UI;
using CustomNetworking.Shared;
using Sandbox;

namespace CustomNetworking.Client;

public class MyGame : Sandbox.Game
{
	public new static MyGame Current => (Sandbox.Game.Current as MyGame)!;

	public readonly NetworkManager? NetworkManager;
	public readonly EntityManager? EntityManager;
	public readonly GameHud? GameHud;

	public MyGame()
	{
		if ( !IsClient )
			return;

		NetworkManager = new NetworkManager();
		NetworkManager.ConnectedToServer += OnConnectedToServer;
		NetworkManager.DisconnectedFromServer += OnDisconnectedFromServer;
		NetworkManager.ClientConnected += OnClientConnected;
		NetworkManager.ClientDisconnected += OnClientDisconnected;
		EntityManager = new EntityManager();
		GameHud = new GameHud();
	}

	[Event.Tick]
	private void Tick()
	{
		if ( NetworkManager is null || !NetworkManager.Connected || EntityManager is null )
			return;
		
		foreach ( var entity in EntityManager.Entities )
			entity.Update();
		
	}
	
	private void OnConnectedToServer()
	{
		Log.Info( "Connected" );
	}
	
	private void OnDisconnectedFromServer()
	{
		Log.Info( "Disconnected" );

		EntityManager?.DeleteAllEntities();
	}
	
	private static void OnClientConnected( INetworkClient client )
	{
		ClientChatBox.AddInformation( $"{client.ClientId} has joined", $"avatar:{client.ClientId}" );
	}
	
	private static void OnClientDisconnected( INetworkClient client )
	{
		ClientChatBox.AddInformation( $"{client.ClientId} has left", $"avatar{client.ClientId}" );
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
