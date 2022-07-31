#if CLIENT
using System.IO;
using System.Numerics;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkEntity
{
	/// <summary>
	/// <see cref="Update"/> but for the client realm.
	/// </summary>
	protected virtual void UpdateClient()
	{
	}
}
#endif
