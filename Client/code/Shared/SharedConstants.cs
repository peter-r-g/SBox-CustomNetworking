namespace CustomNetworking.Shared;

public static class SharedConstants
{
	public const int MaxBufferSize = 65536;
	public const int Port = 7087;
	public const int PartialMessagePayloadSize = 60000;
	
#if SERVER
	public const bool IsServer = true;
	public const bool IsClient = false;
#endif
#if CLIENT
	public const bool IsServer = false;
	public const bool IsClient = true;
#endif
}
