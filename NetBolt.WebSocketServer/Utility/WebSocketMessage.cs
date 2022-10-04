using System;
using System.Diagnostics.Contracts;
using System.Text;
using NetBolt.WebSocket.Enums;
using NetBolt.WebSocket.Extensions;

namespace NetBolt.WebSocket.Utility;

/// <summary>
/// Utility class to work with web socket messages.
/// </summary>
internal static class WebSocketMessage
{
	/// <summary>
	/// Decodes a clients message data portion.
	/// </summary>
	/// <param name="bytes">The bytes containing the whole message.</param>
	/// <param name="dataLength">The length of the data inside the message.</param>
	/// <param name="offset">The offset in the bytes that the data begins.</param>
	/// <returns>The decoded data.</returns>
	[Pure]
	internal static byte[] Decode( ReadOnlySpan<byte> bytes, int dataLength, int offset )
	{
		Span<byte> decoded = stackalloc byte[dataLength];
		var masks = bytes.Slice( offset, 4 );
		offset += 4;

		for ( var i = 0; i < dataLength; ++i )
			decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

		return decoded.ToArray();
	}
	
	/// <summary>
	/// Frames a server message.
	/// </summary>
	/// <param name="opCode">The <see cref="WebSocketOpCode"/> that is being sent.</param>
	/// <param name="dataBytes">The data associated with the message.</param>
	/// <returns>The framed message.</returns>
	[Pure]
	internal static byte[] Frame( WebSocketOpCode opCode, ReadOnlySpan<byte> dataBytes )
	{
		Span<byte> frameBytesSpan = stackalloc byte[dataBytes.Length + 10];
		frameBytesSpan[0] = (byte)(128 + opCode);

		var dataBytesLength = dataBytes.Length;
		int dataIndex;
		switch (dataBytesLength)
		{
			case <= 125:
				frameBytesSpan[1] = (byte)dataBytes.Length;
				dataIndex = 2;
				break;
			case <= 65535:
				frameBytesSpan[1] = 126;
				frameBytesSpan[2] = (byte)((dataBytesLength >> 8) & 255);
				frameBytesSpan[3] = (byte)(dataBytesLength & 255);
				dataIndex = 4;
				break;
			default:
				frameBytesSpan[1] = 127;
				// Array.Length does not support ulong index
				frameBytesSpan[2] = 0;
				frameBytesSpan[3] = 0;
				frameBytesSpan[4] = 0;
				frameBytesSpan[5] = 0;
				frameBytesSpan[6] = (byte)((dataBytesLength >> 24) & 255);
				frameBytesSpan[7] = (byte)((dataBytesLength >> 16) & 255);
				frameBytesSpan[8] = (byte)((dataBytesLength >> 8) & 255);
				frameBytesSpan[9] = (byte)(dataBytesLength & 255);
				dataIndex = 10;
				break;
		}

		var frameData = frameBytesSpan.Slice( dataIndex, dataBytesLength );
		dataBytes.CopyTo( frameData );

		return frameBytesSpan.ToArray();
	}

	/// <summary>
	/// Formats a closing message data.
	/// </summary>
	/// <param name="reason">The reason for the disconnect.</param>
	/// <param name="wordReason">A string version of the disconnect.</param>
	/// <returns>The formatted close message data.</returns>
	[Pure]
	internal static byte[] FormatCloseData( WebSocketDisconnectReason reason, string wordReason = "" )
	{
		var strBytesSpan = string.IsNullOrWhiteSpace( wordReason )
			? Span<byte>.Empty
			: Encoding.UTF8.GetBytes( wordReason );

		var closeCode = (int)reason.GetCloseCode();
		Span<byte> closeDataBytesSpan = new byte[strBytesSpan.Length + 2];
		closeDataBytesSpan[0] = (byte)((closeCode >> 8) & 255);
		closeDataBytesSpan[1] = (byte)(closeCode & 255);

		if ( strBytesSpan.Length <= 0 )
			return closeDataBytesSpan.ToArray();

		var closeDataReasonSpan = closeDataBytesSpan.Slice( 2, closeDataBytesSpan.Length - 2 );
		strBytesSpan.CopyTo( closeDataReasonSpan );

		return closeDataBytesSpan.ToArray();
	}
}
