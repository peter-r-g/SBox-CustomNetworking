using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

public static class Program
{
	public static readonly CancellationTokenSource ProgramCancellation = new();
	
	public static int TickRate { get; private set; } = int.MaxValue;
	private static double TickRateDt => (double)1000 / TickRate;

	
	public static readonly ConcurrentQueue<string> Logger = new();
	private static readonly string LogFileName = Environment.CurrentDirectory + '\\' +
	                                             DateTime.Now.ToString( CultureInfo.CurrentCulture )
		                                             .Replace( ':', '-' ) + ".log";
	
	private static Game.Game? _game;

	private static Thread? _networkingThread;
	private static Task? _drawConsoleTask;
	private static Task? _logTask;

	public static void Main( string[] args )
	{
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		_networkingThread = new Thread( NetworkManager.NetworkingMain );
		_networkingThread.Start();

		_game = new Game.Game();
		_game.Start();
		
		NetworkManager.ClientConnected += OnClientConnected;
		NetworkManager.ClientDisconnected += OnClientDisconnected;

		_drawConsoleTask = Task.Run( DrawConsoleAsync );
		_logTask = Task.Run( LogAsync );

		var sw = Stopwatch.StartNew();
		while ( !ProgramCancellation.IsCancellationRequested )
		{
			// TODO: Cooking the CPU is not a very cool way of doing this
			while ( sw.Elapsed.TotalMilliseconds < TickRateDt )
			{
			}

			Time.Delta = sw.Elapsed.TotalMilliseconds;
			sw.Restart();

			NetworkManager.DispatchIncoming();
			_game?.Update();
			NetworkManager.SendOutgoing();
		}
	}

	private static void OnProcessExit( object? sender, EventArgs e )
	{
		_game?.Shutdown();
		ProgramCancellation.Cancel();

		var tasks = new List<Task>();
		if ( _drawConsoleTask is not null )
			tasks.Add( _drawConsoleTask );

		if ( _logTask is not null )
			tasks.Add( _logTask );
		
		Task.WaitAll( tasks.ToArray() );
		_networkingThread?.Join();
	}

	public static void SetTickRate( int tickRate )
	{
		TickRate = tickRate;
	}

	private static void OnClientConnected( INetworkClient client )
	{
		_game?.OnClientConnected( client );
	}
	
	private static void OnClientDisconnected( INetworkClient client )
	{
		_game?.OnClientDisconnected( client );
	}

	private static async Task DrawConsoleAsync()
	{
		while ( !ProgramCancellation.IsCancellationRequested )
		{
			Console.Clear();
			
			// Save current cursor
			var oldLeft = Console.CursorLeft;
			var oldTop = Console.CursorTop;
		
			// Client count
			Console.SetCursorPosition( 1, 1 );
			Console.Write( $"{NetworkManager.Clients.Count} clients connected" );
			
			// Bot count
			Console.SetCursorPosition( 1, 2 );
			Console.Write( $"{NetworkManager.Bots.Count} bots connected" );
			
			// Map
			Console.SetCursorPosition( 1, 25 );
			Console.Write( "Map: None" );
			
			// Server entities
			Console.SetCursorPosition( 1, 27 );
			Console.Write( $"{_game?.ServerEntityManager.Entities.Count} Server Entities" );
			
			// Shared entities
			Console.SetCursorPosition( 1, 28 );
			Console.Write( $"{_game?.SharedEntityManager.Entities.Count} Networked Entities" );
			
#if DEBUG
			// Debug title
			Console.SetCursorPosition( 1, 12 );
			Console.Write( "Networking Stats" );
			
			// Messages sent
			Console.SetCursorPosition( 1, 13 );
			Console.Write( $"Network Messages Sent: {NetworkManager.MessagesSent}" );
			
			// Messages sent to clients
			Console.SetCursorPosition( 1, 14 );
			Console.Write( $"{NetworkManager.MessagesSentToClients} messages sent to clients" );
			
			// Messages received
			Console.SetCursorPosition( 1, 15 );
			Console.Write( $"{NetworkManager.MessagesReceived} messages received" );
#endif

			// TPS
			var tpsText = $"{Math.Ceiling( 1000 / Time.Delta )} TPS";
			Console.SetCursorPosition( Console.BufferWidth - (tpsText.Length + 1), 1 );
			Console.Write( tpsText );
		
			// Reset
			Console.SetCursorPosition( oldLeft, oldTop );

			await Task.Delay( 1000 );
		}
	}

	private static async Task LogAsync()
	{
		while ( !ProgramCancellation.IsCancellationRequested )
		{
			if ( Logger.TryDequeue( out var message ) )
				await File.AppendAllTextAsync( LogFileName, $"[{DateTime.Now}] {message}\n", ProgramCancellation.Token );
		}
	}
}
