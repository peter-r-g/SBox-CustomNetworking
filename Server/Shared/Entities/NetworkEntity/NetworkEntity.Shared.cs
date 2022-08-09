using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
	private readonly HashSet<string> _changedProperties = new();

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
	/// Marks a property as changed and invokes the <see cref="Changed"/> event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected void TriggerNetworkingChange( [CallerMemberName] string propertyName = "" )
	{
		if ( !_propertyNameCache.ContainsKey( propertyName ) )
			throw new InvalidOperationException( $"\"{propertyName}\" is not a networkable property on {GetType().Name}." );
		
		_changedProperties.Add( propertyName );
		Changed?.Invoke( this, this );
	}
	
	/// <summary>
	/// Called when <see cref="Position"/> has changed.
	/// </summary>
	/// <param name="oldPosition">The old instance of <see cref="Position"/>.</param>
	/// <param name="newPosition">The new instance of <see cref="Position"/>.</param>
	protected virtual void OnPositionChanged( NetworkedVector3 oldPosition, NetworkedVector3 newPosition )
	{
		TriggerNetworkingChange( nameof(Position) );
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
		_ = reader.ReadInt32();
		
		foreach ( var property in _propertyNameCache.Values )
			property.SetValue( this, reader.ReadNetworkable() );
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		var changedCount = reader.ReadInt32();
		for ( var i = 0; i < changedCount; i++ )
		{
			var propertyName = reader.ReadString();
			var currentValue = _propertyNameCache[propertyName].GetValue( this );
			(currentValue as INetworkable)!.DeserializeChanges( reader );
		}
	}
	
	public void Serialize( NetworkWriter writer )
	{
		writer.Write( EntityId );
		
		foreach ( var property in _propertyNameCache.Values )
			writer.WriteNetworkable( (INetworkable)property.GetValue( this )! );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		writer.Write( _changedProperties.Count );
		foreach ( var propertyName in _changedProperties )
		{
			writer.Write( propertyName );
			writer.WriteNetworkableChanges( (INetworkable)_propertyNameCache[propertyName].GetValue( this )! );
		}
		_changedProperties.Clear();
	}
}
