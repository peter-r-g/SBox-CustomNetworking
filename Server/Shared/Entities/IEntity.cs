#if CLIENT
using System;
using CustomNetworking.Client;
#endif
#if SERVER
using CustomNetworking.Server;
#endif
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

/// <summary>
/// Contract to define something that is an entity in the game world.
/// </summary>
public interface IEntity : INetworkable<IEntity>
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
	
#if SERVER
	public static EntityManager All => BaseGame.Current.SharedEntityManager;
	public static EntityManager Local => BaseGame.Current.ServerEntityManager;
#endif
#if CLIENT
	public static EntityManager All
	{
		get
		{
			if ( NetworkManager.Instance is null )
				throw new Exception( "Attempted to access all networked entities when the NetworkManager doesn't exist." );
			
			return NetworkManager.Instance.SharedEntityManager;
		}
	}
#endif
}
