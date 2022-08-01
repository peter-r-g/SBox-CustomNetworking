using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

/// <summary>
/// Contract to define something that is an entity in the game world.
/// </summary>
public interface IEntity : INetworkable
{
	/// <summary>
	/// The unique identifier of the entity.
	/// </summary>
	NetworkedInt EntityId { get; }

	/// <summary>
	/// Deletes this <see cref="IEntity"/>.
	/// <remarks>You should not use this <see cref="IEntity"/> after calling this.</remarks>
	/// </summary>
	void Delete();
	/// <summary>
	/// Logic update for this <see cref="IEntity"/>.
	/// <remarks>This will be called every server tick or client frame.</remarks>
	/// </summary>
	void Update();
}
