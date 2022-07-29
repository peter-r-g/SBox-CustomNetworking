using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

public interface IEntity : INetworkable
{
	NetworkedInt EntityId { get; }

	void Delete();
	void Update();
}
