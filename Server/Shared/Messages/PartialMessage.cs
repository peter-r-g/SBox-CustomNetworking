using System;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public class PartialMessage : NetworkMessage
{
	public Guid MessageGuid { get; private set; }
	public int NumPieces { get; private set; }
	public int Piece { get; private set; }
	public byte[] Data { get; private set; }

	public PartialMessage( Guid messageGuid, int numPieces, int piece, byte[] data )
	{
		MessageGuid = messageGuid;
		NumPieces = numPieces;
		Piece = piece;
		Data = data;
	}

	public override void Deserialize( NetworkReader reader )
	{
		var bytes = new byte[16];
		_ = reader.Read( bytes, 0, 16 );
		MessageGuid = new Guid( bytes );
		
		NumPieces = reader.ReadInt32();
		Piece = reader.ReadInt32();
		
		var dataLength = reader.ReadInt32();
		Data = new byte[dataLength];
		_ = reader.Read( Data, 0, dataLength );
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( nameof(PartialMessage) );
		writer.Write( MessageGuid.ToByteArray() );
		writer.Write( NumPieces );
		writer.Write( Piece );
		writer.Write( Data.Length );
		writer.Write( Data );
	}
}
