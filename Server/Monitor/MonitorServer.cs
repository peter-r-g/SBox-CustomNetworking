using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Shared.Utility;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Messages;

namespace CustomNetworking.Server;

internal sealed class MonitorServer
{
	private TcpListener _tcpListener = null!;
	private readonly List<TcpClient> _clients = new();

	private Task? _acceptClientTask;
	private readonly int _port;

	private List<TcpClient> _disconnectedClients = new();

	internal MonitorServer( int port )
	{
		_port = port;
	}

	internal async void MonitorMain()
	{
		_tcpListener = TcpListener.Create( _port );
		_tcpListener.Start();

		_acceptClientTask = Task.Run( AcceptClientAsync );

		while ( !Program.ProgramCancellation.IsCancellationRequested )
		{
			if ( _clients.Count == 0 )
			{
				await Task.Delay( 1 );
				continue;
			}
			
			var stream = new MemoryStream();
			var writer = new NetworkWriter( stream );
			writer.WriteNetworkable( new ServerInformationMessage() );
			writer.Close();
			var bytes = stream.ToArray();

			foreach ( var client in _clients )
			{
				try
				{
					await client.GetStream().WriteAsync( bytes, CancellationToken.None );
				}
				catch ( IOException )
				{
					_disconnectedClients.Add( client );
				}
			}

			foreach ( var disconnectedClient in _disconnectedClients )
			{
				disconnectedClient.Close();
				_clients.Remove( disconnectedClient );
			}
			_disconnectedClients.Clear();

			await Task.Delay( 5 );
		}

		if ( _acceptClientTask is not null )
			await _acceptClientTask;
		
		foreach ( var client in _clients )
			client.Close();
	}

	private async Task AcceptClientAsync()
	{
		while ( !Program.ProgramCancellation.IsCancellationRequested )
		{
			if ( !_tcpListener.Pending() )
				continue;
			
			_clients.Add( await _tcpListener.AcceptTcpClientAsync() );
		}
	}
}
