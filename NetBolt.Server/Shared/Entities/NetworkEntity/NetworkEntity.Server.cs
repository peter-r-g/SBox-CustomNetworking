#if SERVER
namespace CustomNetworking.Shared.Entities;

public partial class NetworkEntity
{
	/// <summary>
	/// <see cref="Update"/> but for the server realm.
	/// </summary>
	protected virtual void UpdateServer()
	{
	}
}
#endif
