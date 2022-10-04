#if CLIENT
using System;
using CustomNetworking.Client;
using CustomNetworking.Shared.Utility;
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
public interface IEntity : INetworkable
{
	/// <summary>
	/// The unique identifier of the <see cref="IEntity"/>.
	/// </summary>
	int EntityId { get; }
	
	/// <summary>
	/// The <see cref="INetworkClient"/> that owns this <see cref="IEntity"/>.
	/// </summary>
	INetworkClient? Owner { get; set; }

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
	/// <summary>
	/// Contains all networked entities in the server.
	/// </summary>
	public static EntityManager All => BaseGame.Current.SharedEntityManager;
	/// <summary>
	/// Contains all entities that only exist on the server.
	/// </summary>
	public static EntityManager Local => BaseGame.Current.ServerEntityManager;
#endif
#if CLIENT
	/// <summary>
	/// Contains all networked entities in the server.
	/// <remarks>This may not actually contain all networked entities as the server could be limiting this information.</remarks>
	/// </summary>
	public static EntityManager All
	{
		get
		{
			if ( NetworkManager.Instance is null )
			{
				Logging.Error( $"Attempted to access all networked entities when the {nameof(NetworkManager)} doesn't exist." );
				return null!;
			}
			
			return NetworkManager.Instance.SharedEntityManager;
		}
	}
#endif
}
