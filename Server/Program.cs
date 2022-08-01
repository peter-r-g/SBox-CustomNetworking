using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

public static class Program
{
	public static readonly CancellationTokenSource ProgramCancellation = new();

	public static int TickRate = int.MaxValue;
	private static double TickRateDt => (double)1000 / TickRate;

	
	public static readonly ConcurrentQueue<string> Logger = new();
	private static readonly string LogFileName = Environment.CurrentDirectory + '\\' +
	                                             DateTime.Now.ToString( CultureInfo.CurrentCulture )
		                                             .Replace( ':', '-' ) + ".log";

	private static MonitorServer _monitor = null!;
	private static NetworkServer _server = null!;
	private static BaseGame _game = null!;

	private static Thread? _networkingThread;
	private static Thread? _monitorThread;

	public static void Main( string[] args )
	{
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		_monitor = new MonitorServer( SharedConstants.MonitorPort );
		_server = new NetworkServer( SharedConstants.Port );
		NetworkServer.Instance = _server;
		_game = new BaseGame();

		_game.Start();
		
		_monitorThread = new Thread( _monitor.MonitorMain );
		_monitorThread.Start();

		_server.ClientConnected += OnClientConnected;
		_server.ClientDisconnected += OnClientDisconnected;
		_networkingThread = new Thread( _server.NetworkingMain );
		_networkingThread.Start();

		var sw = Stopwatch.StartNew();
		while ( !ProgramCancellation.IsCancellationRequested )
		{
			while ( !Logger.IsEmpty )
			{
				if ( Logger.TryDequeue( out var message ) )
					Console.WriteLine( message );
			}
			
			// TODO: Cooking the CPU is not a very cool way of doing this
			while ( sw.Elapsed.TotalMilliseconds < TickRateDt )
			{
			}

			Time.Delta = sw.Elapsed.TotalMilliseconds;
			sw.Restart();
			
			_server.DispatchIncoming();
			_game?.Update();
			_server.DispatchOutgoing();
		}
	}

	private static void OnProcessExit( object? sender, EventArgs e )
	{
		_game.Shutdown();
		ProgramCancellation.Cancel();
		
		_server.ClientConnected -= OnClientConnected;
		_server.ClientDisconnected -= OnClientDisconnected;
		
		_networkingThread?.Join();
		_monitorThread?.Join();
	}

	private static void OnClientConnected( INetworkClient client )
	{
		_game.OnClientConnected( client );
	}
	
	private static void OnClientDisconnected( INetworkClient client )
	{
		_game.OnClientDisconnected( client );
	}
}
