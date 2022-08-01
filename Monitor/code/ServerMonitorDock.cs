using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CustomNetworking.Server;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Utility;
using Tools;

namespace CustomNetworking.Monitor;

[Dock( "Editor", "Server Monitor", "snippet_folder" )]
public class ServerMonitorDock : Widget
{
	public static ServerMonitorDock Instance;

	private bool Connected => _client is not null && _client.Connected;
	private bool Connecting => _client is not null && !_client.Connected;
	
	private CancellationTokenSource _cancellationTokenSource = new();
	private TcpClient? _client;
	private Task? _startClientTask;
	private Task? _receiveTask;
	
	private readonly LineEdit _commandEntry;
	private readonly Button _connectionButton;

	private ServerInformationMessage? _lastInfo;

	public ServerMonitorDock( Widget parent ) : base( parent )
	{
		Instance = this;

		SetLayout( LayoutMode.TopToBottom );
		Layout.Margin = 4;
		Layout.Spacing = 4;
		
		var column = Layout.Column();
		column.VerticalSpacing = 4;
		
		_commandEntry = new LineEdit
		{
			Alignment = TextFlag.LeftCenter,
			PlaceholderText = "Enter Command.."
		};

		_connectionButton = new Button.Primary( "Connect" );
		_connectionButton.Clicked += () =>
		{
			if ( Connected )
				DisconnectButtonClicked();
			else
				ConnectButtonClicked();
		};

		var connectionRow = column.Add( LayoutMode.LeftToRight );
		connectionRow.Spacing = 8;
		connectionRow.Margin = 5;
		connectionRow.Add( _commandEntry );
		connectionRow.Add( _connectionButton );

		Layout.Add( new MessageListView() );
		Layout.Add( column );

		var infoColumn = Layout.Column();
		infoColumn.AddStretchCell( 1 );
		Layout.Add( infoColumn );
	}

	protected override void OnPaint()
	{
		base.OnPaint();
		
		Paint.ClearPen();
		Paint.SetBrush( new Color( 0.1f, 0.1f, 0.1f ) );
		Paint.DrawRect( LocalRect );

		var connected = Connected;
		_connectionButton.Text = connected ? "Disconnect" : Connecting ? "Connecting" : "Connect";
		_connectionButton.Enabled = !Connecting;
		_commandEntry.Enabled = connected;

		if ( _lastInfo is null )
			return;
		
		Paint.SetDefaultFont();
		var textContract = LocalRect.Contract( LocalRect.width - 50, 0 );
		textContract.width = 50;
		textContract.height = LocalRect.height;

		var clientsConnectedContract = textContract.Contract( 5, 5 );
		clientsConnectedContract.width = textContract.width - 5;
		Paint.DrawText( clientsConnectedContract, $"{_lastInfo.NumClientsConnected} clients connected" );
	}

	private void ConnectButtonClicked()
	{
		if ( _startClientTask is not null && !_startClientTask.IsCompleted )
			return;
		
		if ( _client is not null )
			StopClient();
		
		_startClientTask = StartClient();
		Task.Run( StartClientTimeout );
	}

	private void DisconnectButtonClicked()
	{
		StopClient();
	}

	private async Task StartClient()
	{
		_cancellationTokenSource = new CancellationTokenSource();
		_client = new TcpClient();

		await _client.ConnectAsync( IPAddress.Parse( "127.0.0.1" ), SharedConstants.MonitorPort, _cancellationTokenSource.Token );
		_receiveTask = Task.Run( ReceiveDataAsync );
	}

	private async Task StartClientTimeout()
	{
		await Task.Delay( TimeSpan.FromSeconds( 10 ) );
		
		if ( _client is not null && !_client.Connected )
		{
			StopClient();
			throw new TimeoutException( "Connection to monitor server timed out." );
		}
	}

	private void StopClient()
	{
		_cancellationTokenSource.Cancel();
		_receiveTask?.Wait();
		_client?.Close();
		_client = null;
	}

	private async Task ReceiveDataAsync()
	{
		while ( !_cancellationTokenSource.IsCancellationRequested )
		{
			if ( _client is null )
				return;
			
			if ( _client.Available == 0 )
			{
				await Task.Delay( 1 );
				continue;
			}

			try
			{
				var bytes = new byte[_client.Available];
				_ = await _client.GetStream().ReadAsync( bytes, 0, bytes.Length, _cancellationTokenSource.Token );
				var reader = new NetworkReader( new MemoryStream( bytes ) );
				var message = NetworkMessage.DeserializeMessage( reader );
				reader.Close();
				DispatchMessage( message );
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}
	}

	private void DispatchMessage( NetworkMessage message )
	{
		switch ( message )
		{
			case ServerInformationMessage serverInfo:
				_lastInfo = serverInfo;
				break;
		}
	}

	public class MessageListView : ListView
	{
		
	}
}
