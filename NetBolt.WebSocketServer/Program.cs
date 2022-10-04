using System;
using System.Net;

namespace NetBolt.WebSocket;

public static class Program
{
	private const string Ip = "127.0.0.1";
	private const int Port = 9987;

	private static WebSocketServer? _server;
	
	public static void Main( string[] args )
	{
		_server = new WebSocketServer( IPAddress.Parse( Ip ), Port );
		_server.Start();
		Console.WriteLine( "Server started on {0}:{1}", Ip, Port );

		Console.WriteLine( "Press [ENTER] to close" );
		Console.ReadLine();
		
		Console.WriteLine( "Shutting down..." );
		_server.StopAsync().Wait();
	}
}
