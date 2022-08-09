using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Entities;

/// <summary>
/// The base class for all of your entity needs.
/// </summary>
public partial class NetworkEntity : IEntity
{
	public event INetworkable<IEntity>.ChangedEventHandler? Changed;
	public NetworkedInt EntityId { get; }

	/// <summary>
	/// The world position of the <see cref="NetworkEntity"/>.
	/// </summary>
	public NetworkedVector3 Position
	{
		get => _position;
		set
		{
			var oldPosition = _position;
			_position.Changed -= OnPositionChanged;
			_position = value;
			value.Changed += OnPositionChanged;
			OnPositionChanged( oldPosition, value );
		}
	}
	private NetworkedVector3 _position;

	/// <summary>
	/// The velocity of the <see cref="NetworkEntity"/>.
	/// </summary>
	public NetworkedVector3 Velocity
	{
		get => _velocity;
		set
		{
			var oldVelocity = _velocity;
			_velocity.Changed -= OnVelocityChanged;
			_velocity = value;
			value.Changed += OnVelocityChanged;
			OnVelocityChanged( oldVelocity, value );
		}
	}
	private NetworkedVector3 _velocity;

	private readonly Dictionary<string, PropertyInfo> _propertyNameCache = new();

	public NetworkEntity( int entityId )
	{
		EntityId = entityId;
		
		foreach ( var property in GetType().GetProperties()
			         .Where( property => property.PropertyType.IsAssignableTo( typeof(INetworkable) ) ) )
		{
			if ( property.Name == nameof(EntityId) )
				continue;
			
			_propertyNameCache.Add( property.Name, property );
		}
	}

	public virtual void Delete()
	{
	}
	
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
	/// Called when <see cref="Position"/> has changed.
	/// </summary>
	/// <param name="oldPosition">The old instance of <see cref="Position"/>.</param>
	/// <param name="newPosition">The new instance of <see cref="Position"/>.</param>
	protected virtual void OnPositionChanged( NetworkedVector3 oldPosition, NetworkedVector3 newPosition )
	{
#if SERVER
		TriggerNetworkingChange( nameof(Position) );
#endif
	}

	/// <summary>
	/// Called when <see cref="Velocity"/> has changed.
	/// </summary>
	/// <param name="oldVelocity">The old instance of <see cref="Velocity"/>.</param>
	/// <param name="newVelocity">The new instance of <see cref="Velocity"/>.</param>
	protected virtual void OnVelocityChanged( NetworkedVector3 oldVelocity, NetworkedVector3 newVelocity )
	{
#if SERVER
		TriggerNetworkingChange( nameof(Velocity) );
#endif
	}

	public void Deserialize( NetworkReader reader )
	{
#if CLIENT
		_ = reader.ReadInt32();
		
		foreach ( var property in _propertyNameCache.Values )
			property.SetValue( this, reader.ReadNetworkable() );
#endif
	}

	public void DeserializeChanges( NetworkReader reader )
	{
#if CLIENT
		var changedCount = reader.ReadInt32();
		for ( var i = 0; i < changedCount; i++ )
		{
			var propertyName = reader.ReadString();
			var currentValue = _propertyNameCache[propertyName].GetValue( this );
			(currentValue as INetworkable)!.DeserializeChanges( reader );
		}
#endif
	}
	
	public void Serialize( NetworkWriter writer )
	{
#if SERVER
		writer.Write( EntityId );
		
		foreach ( var property in _propertyNameCache.Values )
			writer.WriteNetworkable( (INetworkable)property.GetValue( this )! );
#endif
	}

	public void SerializeChanges( NetworkWriter writer )
	{
#if SERVER
		writer.Write( _changedProperties.Count );
		foreach ( var propertyName in _changedProperties )
		{
			writer.Write( propertyName );
			writer.WriteNetworkableChanges( (INetworkable)_propertyNameCache[propertyName].GetValue( this )! );
		}
		_changedProperties.Clear();
#endif
	}
}
