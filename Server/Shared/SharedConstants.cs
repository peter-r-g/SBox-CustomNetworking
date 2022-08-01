namespace CustomNetworking.Shared;

/// <summary>
/// Contains constants for both realms to be aware about.
/// </summary>
public static class SharedConstants
{
	/// <summary>
	/// The max byte size of any data going between server and client.
	/// </summary>
	public const int MaxBufferSize = 65536;
	/// <summary>
	/// The port the game server will run on.
	/// </summary>
	public const int Port = 7087;
	/// <summary>
	/// The port the monitor server will run on.
	/// </summary>
	public const int MonitorPort = Port + 1;
	
#if SERVER
	/// <summary>
	/// Whether we're in the server realm or not.
	/// </summary>
	public const bool IsServer = true;
	/// <summary>
	/// Whether we're in the client realm or not.
	/// </summary>
	public const bool IsClient = false;
#endif
#if CLIENT
	/// <summary>
	/// Whether we're in the server realm or not.
	/// </summary>
	public const bool IsServer = false;
	/// <summary>
	/// Whether we're in the client realm or not.
	/// </summary>
	public const bool IsClient = true;
#endif
}
