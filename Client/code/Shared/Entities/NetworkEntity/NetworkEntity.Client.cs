#if CLIENT
using System.IO;
using System.Numerics;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkEntity
{
	protected virtual void UpdateClient()
	{
	}
}
#endif
