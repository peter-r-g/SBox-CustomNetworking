using System.Buffers;
using CustomNetworking.Client.UI;
using CustomNetworking.Game;
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
		NetworkManager.Connected += OnConnected;
		NetworkManager.Disconnected += OnDisconnected;
		NetworkManager.ClientConnected += OnClientConnected;
		NetworkManager.ClientDisconnected += OnClientDisconnected;
		NetworkManager.HandleMessage<SayMessage>( HandleSayMessage );
		EntityManager = new EntityManager();
		GameHud = new GameHud();
	}

	[Event.Tick]
	private void Tick()
	{
		if ( EntityManager is null )
			return;
		
		foreach ( var entity in EntityManager.Entities )
			entity.Update();
	}
	
	private void OnConnected()
	{
		Log.Info( "Connected" );
	}
	
	private void OnDisconnected()
	{
		Log.Info( "Disconnected" );

		if ( EntityManager is null )
			return;
		
		foreach ( var entity in EntityManager.Entities )
			entity.Delete();
		EntityManager?.Entities.Clear();
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
