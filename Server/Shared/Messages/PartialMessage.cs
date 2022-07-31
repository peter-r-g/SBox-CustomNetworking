using System;
#if CLIENT
using System.Buffers;
#endif
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class PartialMessage : NetworkMessage
{
	public const int MaxPayloadSize = SharedConstants.MaxBufferSize - 16 - sizeof(int) - sizeof(int) - 100;
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

#if CLIENT
	~PartialMessage()
	{
		ArrayPool<byte>.Shared.Return( Data, true );
	}
#endif

	public override void Deserialize( NetworkReader reader )
	{
#if CLIENT
		var bytes = new byte[16];
		_ = reader.Read( bytes, 0, 16 );
		MessageGuid = new Guid( bytes );
		
		NumPieces = reader.ReadInt32();
		Piece = reader.ReadInt32();
		
		var dataLength = reader.ReadInt32();
		Data = ArrayPool<byte>.Shared.Rent( dataLength );
		_ = reader.Read( Data, 0, dataLength );
#endif
	}

	public override void Serialize( NetworkWriter writer )
	{
#if SERVER
		writer.Write( MessageGuid.ToByteArray() );
		writer.Write( NumPieces );
		writer.Write( Piece );
		writer.Write( Data.Length );
		writer.Write( Data );
#endif
	}
}
