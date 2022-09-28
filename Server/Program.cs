using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Utility;

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
	private static float TickRateDt => (float)1000 / TickRate;
	
	private static NetworkServer _server = null!;
	private static Task? _serverTask;
	private static BaseGame _game = null!;

	public static void Main( string[] args )
	{
		Logging.Initialize();
		Logging.Info( "Log started" );
		
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		_server = new NetworkServer( SharedConstants.Port, true );
		_serverTask = _server.Start();
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
			_server.DispatchOutgoing();
		}
	}

	private static void OnProcessExit( object? sender, EventArgs e )
	{
		_game.Shutdown();
		ProgramCancellation.Cancel();
		_serverTask?.Wait();
		
		Logging.Dispose();
	}
}
