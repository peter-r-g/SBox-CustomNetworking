using System;
using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;

namespace CustomNetworking.Game;

public class Game
{
	public const int BotLimit = 1000;

	public readonly EntityManager ServerEntityManager = new();
	public readonly EntityManager SharedEntityManager = new();

	public GameInformationEntity GameInformationEntity;
	
	public void Start()
	{
		Program.SetTickRate( 60 );
		NetworkManager.HandleMessage<SayMessage>( HandleSayMessage );
		
		BotClient.HandleBotMessage<PartialMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<ClientListMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<EntityListMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<ClientStateChangedMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<EntityUpdateMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<SayMessage>( DumpBotMessage );

		GameInformationEntity = SharedEntityManager.Create<GameInformationEntity>();
		Task.Run( AddBotLoopAsync );
		for ( var i = 0; i < 5; i++ )
			SharedEntityManager.Create<TestCitizenEntity>();
	}

	public void Update()
	{
		foreach ( var serverEntity in ServerEntityManager.Entities )
			serverEntity.Update();
		
		foreach ( var sharedEntity in SharedEntityManager.Entities )
			sharedEntity.Update();

		foreach ( var sharedEntity in SharedEntityManager.Entities )
		{
			if ( !sharedEntity.HasChanged )
				continue;
			
			NetworkManager.QueueMessage( To.All, new EntityUpdateMessage( sharedEntity ) );
		}
	}

	public void OnClientConnected( INetworkClient client )
	{
		var toClient = To.Single( client );
		NetworkManager.QueueMessage( toClient, new ClientListMessage( NetworkManager.Clients.Keys ) );
		NetworkManager.QueueMessage( toClient, new EntityListMessage( SharedEntityManager.Entities ) );
		NetworkManager.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Connected ) );
		
		Program.Logger.Enqueue( $"{client.ClientId} has connected" );
	}

	public void OnClientDisconnected( INetworkClient client )
	{
		var message = new ClientStateChangedMessage( client.ClientId, ClientState.Disconnected );
		NetworkManager.QueueMessage( To.AllExcept( client ), message );
		
		Program.Logger.Enqueue( $"{client.ClientId} has disconnected" );
	}
	
	private static async Task AddBotLoopAsync()
	{
		while ( !Program.ProgramCancellation.IsCancellationRequested && NetworkManager.Bots.Count < BotLimit )
		{
			NetworkManager.AcceptClient( Math.Abs( Random.Shared.NextInt64() ) );
			await Task.Delay( 1 );
		}
	}

	private static void HandleSayMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not SayMessage sayMessage )
			return;
		
		NetworkManager.QueueMessage( To.AllExcept( client ), new SayMessage( client, sayMessage.Message ) );
		Program.Logger.Enqueue( $"{client.ClientId}: {sayMessage.Message}" );
	}

	private static void DumpBotMessage( BotClient client, NetworkMessage message )
	{
	}
}
