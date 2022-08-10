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
			OnPositionChanged( oldPosition, value );
		}
	}
	private NetworkedVector3 _position;
	
	/// <summary>
	/// The world rotation of the <see cref="NetworkModelEntity"/>.
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
			OnRotationChanged( oldRotation, value );
		}
	}
	private NetworkedQuaternion _rotation;

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
	/// Called when ownership of the entity has changed.
	/// </summary>
	/// <param name="oldOwner">The old owner of the entity.</param>
	/// <param name="newOwner">The new owner of the entity.</param>
	protected virtual void OnOwnerChanged( INetworkClient oldOwner, INetworkClient newOwner )
	{
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
	/// Called when <see cref="Rotation"/> has changed.
	/// </summary>
	/// <param name="oldRotation">The old instance of <see cref="Rotation"/>.</param>
	/// <param name="newRotation">The new instance of <see cref="Rotation"/>.</param>
	protected virtual void OnRotationChanged( NetworkedQuaternion oldRotation, NetworkedQuaternion newRotation )
	{
		TriggerNetworkingChange( nameof(Rotation) );
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
