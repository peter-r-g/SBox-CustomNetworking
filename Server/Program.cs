using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;

namespace CustomNetworking.Server;

public static class Program
{
	public const int BotLimit = 1000;
	
	public static readonly ConcurrentQueue<string> Logger = new();
	public static readonly CancellationTokenSource ProgramCancellation = new();

	public static void Main( string[] args )
	{
		var networkingThread = new Thread( NetworkManager.NetworkingMain );
		networkingThread.Start();
		
		NetworkManager.ClientConnected += OnClientConnected;
		NetworkManager.ClientDisconnected += OnClientDisconnected;
		NetworkManager.HandleMessage<SayMessage>( HandleSayMessage );
		
		BotClient.HandleBotMessage<ClientListMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<ClientStateChangedMessage>( DumpBotMessage );
		BotClient.HandleBotMessage<SayMessage>( DumpBotMessage );

		Task.Run( AddBotLoopAsync );
		
		while ( !ProgramCancellation.IsCancellationRequested )
		{
			while ( !Logger.IsEmpty )
			{
				if ( Logger.TryDequeue( out var message ) )
					Console.WriteLine( message );
			}
			
			NetworkManager.DispatchIncoming();
			Update();
			NetworkManager.SendOutgoing();
			
			Thread.Sleep( 1 );
		}
	}

	private static void Update()
	{
		foreach ( var (_, bot) in NetworkManager.Bots )
			bot.Think();
	}

	private static async Task AddBotLoopAsync()
	{
		while ( !ProgramCancellation.IsCancellationRequested && NetworkManager.Bots.Count < BotLimit )
		{
			NetworkManager.AcceptClient( Math.Abs( Random.Shared.NextInt64() ) );
			await Task.Delay( 10 );
		}
	}

	private static void OnClientConnected( INetworkClient client )
	{
		NetworkManager.QueueMessage( To.Single( client ), new ClientListMessage( NetworkManager.Clients.Keys ) );
		NetworkManager.QueueMessage( To.AllExcept( client ), new ClientStateChangedMessage( client.ClientId, ClientState.Connected ) );
		
		Logger.Enqueue( $"{client.ClientId} has connected" );
	}

	private static void OnClientDisconnected( INetworkClient client )
	{
		var message = new ClientStateChangedMessage( client.ClientId, ClientState.Disconnected );
		NetworkManager.QueueMessage( To.AllExcept( client ), message );
		
		Logger.Enqueue( $"{client.ClientId} has disconnected" );
	}

	private static void HandleSayMessage( INetworkClient client, NetworkMessage message )
	{
		if ( message is not SayMessage sayMessage )
			return;
		
		NetworkManager.QueueMessage( To.AllExcept( client ), new SayMessage( client, sayMessage.Message ) );
		Logger.Enqueue( $"{client.ClientId}: {sayMessage.Message}" );
	}

	private static void DumpBotMessage( BotClient client, NetworkMessage message )
	{
	}
}
