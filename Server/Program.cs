using System;
using System.Diagnostics;
using System.Threading;
using CustomNetworking.Shared;

namespace CustomNetworking.Server;

public static class Program
{
	/// <summary>
	/// The whole programs cancellation source. If you want to exit the program then cancel this and the program will exit at the end of the tick.
	/// </summary>
	public static readonly CancellationTokenSource ProgramCancellation = new();

	/// <summary>
	/// The target tick rate for the server.
	/// </summary>
	public static int TickRate = int.MaxValue;
	private static double TickRateDt => (double)1000 / TickRate;

	private static MonitorServer _monitor = null!;
	private static NetworkServer _server = null!;
	private static BaseGame _game = null!;

	private static Thread? _networkingThread;
	private static Thread? _monitorThread;

	public static void Main( string[] args )
	{
		Logging.Initialize();
		Logging.Info( "Log started" );
		
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		_monitor = new MonitorServer( SharedConstants.MonitorPort );
		_server = new NetworkServer( SharedConstants.Port, true );
		NetworkServer.Instance = _server;
		_game = new BaseGame();

		_game.Start();
		
		_monitorThread = new Thread( _monitor.MonitorMain );
		_monitorThread.Start();

		_server.ClientConnected += _game.OnClientConnected;
		_server.ClientDisconnected += _game.OnClientDisconnected;
		_networkingThread = new Thread( _server.NetworkingMain );
		_networkingThread.Start();

		var sw = Stopwatch.StartNew();
		while ( !ProgramCancellation.IsCancellationRequested )
		{
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
		
		_server.ClientConnected -= _game.OnClientConnected;
		_server.ClientDisconnected -= _game.OnClientDisconnected;
		
		_networkingThread?.Join();
		_monitorThread?.Join();
		
		Logging.Dispose();
	}
}
