using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A client to server <see cref="NetworkMessage"/> containing information about updates to a <see cref="INetworkClient"/>s pawn.
/// </summary>
public sealed class ClientPawnUpdateMessage : NetworkMessage
{
	/// <summary>
	/// Contains all data changes for the clients pawn.
	/// </summary>
	public byte[] PartialPawnData { get; private set; } = null!;

#if CLIENT
	public ClientPawnUpdateMessage( byte[] partialPawnData )
	{
		PartialPawnData = partialPawnData;
	}
#endif
	
	public override void Deserialize( NetworkReader reader )
	{
		PartialPawnData = new byte[reader.ReadInt32()];
		_ = reader.Read( PartialPawnData, 0, PartialPawnData.Length );
;	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( PartialPawnData.Length );
		writer.Write( PartialPawnData );
	}
}
