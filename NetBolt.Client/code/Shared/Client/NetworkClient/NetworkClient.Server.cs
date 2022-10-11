#if SERVER
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetBolt.Server;
using NetBolt.Shared.Messages;
using NetBolt.Shared.Utility;
using NetBolt.WebSocket;

namespace NetBolt.Shared;

public partial class NetworkClient
{
	internal NetworkClient( TcpClient socket, IWebSocketServer server ) : base( socket, server )
	{
	}
	
	public void QueueSend( NetworkMessage message )
	{
		var stream = new MemoryStream();
		var writer = new NetworkWriter( stream );
		writer.WriteNetworkable( message );
		writer.Close();
		
		QueueSend( stream.ToArray() );
	}

	protected override void OnData( ReadOnlySpan<byte> bytes )
	{
		base.OnData( bytes );

		var reader = new NetworkReader( new MemoryStream( bytes.ToArray() ) );
		var message = NetworkMessage.DeserializeMessage( reader );
		reader.Close();
		
		NetworkServer.Instance.QueueIncoming( this, message );
	}

	protected override ValueTask<bool> VerifyHandshake( IReadOnlyDictionary<string, string> headers, string request )
	{
		if ( !headers.TryGetValue( "Steam", out var clientIdStr ) )
			return new ValueTask<bool>( false );

		if ( !long.TryParse( clientIdStr, out var clientId ) )
			return new ValueTask<bool>( false );

		if ( NetworkServer.Instance.GetClientById( clientId ) is not null )
			return new ValueTask<bool>( false );

		ClientId = clientId;
		return base.VerifyHandshake( headers, request );
	}
}
#endif
