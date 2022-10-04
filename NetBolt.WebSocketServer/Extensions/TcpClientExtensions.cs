using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace NetBolt.WebSocket.Extensions;

/// <summary>
/// Extension class for <see cref="TcpClient"/>.
/// </summary>
internal static class TcpClientExtensions
{
	/// <summary>
	/// Attempts to get the <see cref="NetworkStream"/> on a <see cref="TcpClient"/>.
	/// </summary>
	/// <param name="client">The <see cref="TcpClient"/> to get the <see cref="NetworkStream"/> of.</param>
	/// <param name="stream">The returned <see cref="NetworkStream"/>.</param>
	/// <returns>Whether or not the stream was obtained.</returns>
	internal static bool TryGetStream( this TcpClient client, [NotNullWhen( true )] out NetworkStream? stream )
	{
		try
		{
			stream = client.GetStream();
			return true;
		}
		catch ( ObjectDisposedException )
		{
			stream = null;
			return false;
		}
	}
}
