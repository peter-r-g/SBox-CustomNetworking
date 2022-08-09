using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Server;

public sealed class ServerInformationMessage : NetworkMessage
{
	public int NumClientsConnected { get; private set; }
	public int MessagesReceived { get; private set; }
	public int MessagesSent { get; private set; }
	public int MessagesSentToClients { get; private set; }
	public int NumServerEntities { get; private set; }
	public int NumNetworkedEntities { get; private set; }
	public double DeltaTime { get; private set; }

#if SERVER
	public ServerInformationMessage()
	{
		NumClientsConnected = NetworkServer.Instance.Clients.Count;
		MessagesReceived = NetworkServer.Instance.MessagesReceived;
		MessagesSent = NetworkServer.Instance.MessagesSent;
		MessagesSentToClients = NetworkServer.Instance.MessagesSentToClients;
		NumServerEntities = IEntity.Local.Entities.Count;
		NumNetworkedEntities = IEntity.All.Entities.Count;
		DeltaTime = Time.Delta;
	}
#endif

	public override void Deserialize( NetworkReader reader )
	{
		NumClientsConnected = reader.ReadInt32();
		MessagesReceived = reader.ReadInt32();
		MessagesSent = reader.ReadInt32();
		MessagesSentToClients = reader.ReadInt32();
		NumServerEntities = reader.ReadInt32();
		NumNetworkedEntities = reader.ReadInt32();
		DeltaTime = reader.ReadDouble();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( NumClientsConnected );
		writer.Write( MessagesReceived );
		writer.Write( MessagesSent );
		writer.Write( MessagesSentToClients );
		writer.Write( NumServerEntities );
		writer.Write( NumNetworkedEntities );
		writer.Write( DeltaTime );
	}
}
