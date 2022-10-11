using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NetBolt.Shared;
using NetBolt.Shared.Utility;
using NetBolt.WebSocket.Options;

namespace NetBolt.Server;

/// <summary>
/// Bootstraps the server.
/// </summary>
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
	/// <summary>
	/// The target delta time for the server.
	/// </summary>
	private static float TickRateDt => (float)1000 / TickRate;

	/// <summary>
	/// The network server handling communication of the game.
	/// </summary>
	private static NetworkServer _server = null!;
	/// <summary>
	/// The game to run.
	/// </summary>
	private static BaseGame _game = null!;

	/// <summary>
	/// The entry point to the program.
	/// </summary>
	/// <param name="args">The command line arguments.</param>
	public static void Main( string[] args )
	{
		Logging.Initialize();
		Logging.Info( "Log started" );

		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		_server = new NetworkServer( WebSocketServerOptions.Default.WithPort( SharedConstants.Port ) );
		NetworkServer.Instance = _server;
		_server.Start();
		_game = new BaseGame();
		_game.Start();

		var sw = Stopwatch.StartNew();
		while ( !ProgramCancellation.IsCancellationRequested )
		{
			// TODO: Cooking the CPU is not a very cool way of doing this
			while ( sw.Elapsed.TotalMilliseconds < TickRateDt )
			{
			}

			Time.Delta = (float)sw.Elapsed.TotalMilliseconds;
			sw.Restart();

			_server.DispatchIncoming();
			_game?.Update();
		}
	}

	/// <summary>
	/// Handler for when the program is shutting down.
	/// </summary>
	private static void OnProcessExit( object? sender, EventArgs e )
	{
		Logging.Info( "Shutting down..." );
		_game.Shutdown();
		ProgramCancellation.Cancel();
		_server.StopAsync().Wait();

		Logging.Info( "Log finished" );
		Logging.Dispose();
	}

	private static void OnUnhandledException( object sender, UnhandledExceptionEventArgs e )
	{
		Logging.Fatal( (Exception)e.ExceptionObject );
		OnProcessExit( null, EventArgs.Empty );
	}
}
