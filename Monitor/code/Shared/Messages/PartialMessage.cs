using System;
#if CLIENT
using System.Buffers;
#endif
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A <see cref="NetworkMessage"/> that contains data that is a part of a bigger <see cref="NetworkMessage"/>.
/// </summary>
public sealed class PartialMessage : NetworkMessage
{
	/// <summary>
	/// The max amount of data that can be packed into a <see cref="PartialMessage"/>.
	/// </summary>
	public const int MaxPayloadSize = SharedConstants.MaxBufferSize - 16 - sizeof(int) - sizeof(int) - 100;

	/// <summary>
	/// The unique identifier of the set of <see cref="PartialMessage"/> this instance is a part of.
	/// </summary>
	public Guid MessageGuid { get; private set; }
	/// <summary>
	/// The number of pieces the set of <see cref="PartialMessage"/> has.
	/// </summary>
	public int NumPieces { get; private set; }
	/// <summary>
	/// The piece this <see cref="PartialMessage"/> is.
	/// </summary>
	public int Piece { get; private set; }
	/// <summary>
	/// The <see cref="NetworkMessage"/> data this <see cref="PartialMessage"/> has.
	/// </summary>
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
