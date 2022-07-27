using System;
using System.IO;

namespace CustomNetworking.Shared.Messages;

public class PartialMessage : NetworkMessage
{
	public readonly Guid MessageGuid;
	public readonly int NumPieces;
	public readonly int Piece;
	public readonly byte[] Data;

	public PartialMessage( Guid messageGuid, int numPieces, int piece, byte[] data )
	{
		MessageGuid = messageGuid;
		NumPieces = numPieces;
		Piece = piece;
		Data = data;
	}

	public PartialMessage( BinaryReader reader )
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

	public override void Serialize( BinaryWriter writer )
	{
		writer.Write( nameof(PartialMessage) );
		writer.Write( MessageGuid.ToByteArray() );
		writer.Write( NumPieces );
		writer.Write( Piece );
		writer.Write( Data.Length );
		writer.Write( Data );
	}
}
