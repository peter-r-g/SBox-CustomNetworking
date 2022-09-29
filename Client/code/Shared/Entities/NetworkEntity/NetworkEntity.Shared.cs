#if CLIENT
using System.Reflection;
#endif
using System;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Entities;

/// <summary>
/// The base class for all of your entity needs.
/// </summary>
public partial class NetworkEntity : BaseNetworkable, IEntity
{
	public new event EventHandler? Changed;
	
	public int EntityId { get; }

	public INetworkClient? Owner
	{
		get => _owner;
		set
		{
			var oldOwner = _owner;
			_owner = value;
			OnOwnerChanged( oldOwner, value );
		}
	}
	private INetworkClient? _owner;

	/// <summary>
	/// The world position of the <see cref="NetworkEntity"/>.
	/// </summary>
	[ClientAuthority]
	public NetworkedVector3 Position
	{
		get => _position;
		set
		{
			var oldPosition = _position;
			_position.Changed -= OnPositionChanged;
			_position = value;
			value.Changed += OnPositionChanged;
			OnPositionChanged( value, EventArgs.Empty );
		}
	}
	private NetworkedVector3 _position;
	
	/// <summary>
	/// The world rotation of the <see cref="NetworkEntity"/>.
	/// </summary>
	[ClientAuthority]
	public NetworkedQuaternion Rotation
	{
		get => _rotation;
		set
		{
			var oldRotation = _rotation;
			_rotation.Changed -= OnRotationChanged;
			_rotation = value;
			value.Changed += OnRotationChanged;
			OnRotationChanged( value, EventArgs.Empty );
		}
	}
	private NetworkedQuaternion _rotation;

	public NetworkEntity( int entityId )
	{
		EntityId = entityId;
	}

	/// <summary>
	/// Deletes this entity. You should not use it after calling this.
	/// </summary>
	public virtual void Delete()
	{
	}
	
	/// <summary>
	/// Updates the entity.
	/// </summary>
	public virtual void Update()
	{
#if SERVER
		UpdateServer();
#endif
#if CLIENT
		UpdateClient();
#endif
	}

	/// <summary>
	/// Called when ownership of the entity has changed.
	/// </summary>
	/// <param name="oldOwner">The old owner of the entity.</param>
	/// <param name="newOwner">The new owner of the entity.</param>
	protected virtual void OnOwnerChanged( INetworkClient? oldOwner, INetworkClient? newOwner )
	{
	}
	
	/// <summary>
	/// Called when <see cref="Position"/> has changed.
	/// </summary>
	protected virtual void OnPositionChanged( object? sender, EventArgs args )
	{
		TriggerNetworkingChange( nameof(Position) );
	}
	
	/// <summary>
	/// Called when <see cref="Rotation"/> has changed.
	/// </summary>
	protected virtual void OnRotationChanged( object? sender, EventArgs args )
	{
		TriggerNetworkingChange( nameof(Rotation) );
	}

	protected override void TriggerNetworkingChange( string propertyName = "" )
	{
		base.TriggerNetworkingChange( propertyName );
		
		Changed?.Invoke( this, EventArgs.Empty );
	}

	public sealed override void DeserializeChanges( NetworkReader reader )
	{
		var changedCount = reader.ReadInt32();
		for ( var i = 0; i < changedCount; i++ )
		{
			var property = PropertyNameCache[reader.ReadString()];
#if CLIENT
			if ( Owner == INetworkClient.Local && property.GetCustomAttribute<ClientAuthorityAttribute>() is not null )
			{
				// TODO: What a cunt of a workaround
				TypeHelper.Create<INetworkable>( property.PropertyType )!.DeserializeChanges( reader );
				continue;
			}
#endif
			
			var currentValue = property.GetValue( this );
			(currentValue as INetworkable)!.DeserializeChanges( reader );
			property.SetValue( this, currentValue );
			TriggerNetworkingChange( property.Name );
		}
	}
}
