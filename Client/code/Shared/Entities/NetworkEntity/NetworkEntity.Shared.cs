using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkEntity : IEntity
{
	public event INetworkable.ChangedEventHandler? Changed;
	public NetworkedInt EntityId { get; }

	public NetworkedVector3 Position
	{
		get => _position;
		set
		{
			_position = value;
#if SERVER
			TriggerNetworkingChange( nameof(Position) );
#endif
		}
	}
	private NetworkedVector3 _position;

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
			_propertyNameCache[propertyName].SetValue( this, reader.ReadNetworkable() );
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
			writer.WriteNetworkable( (INetworkable)_propertyNameCache[propertyName].GetValue( this )! );
		}
		_changedProperties.Clear();
#endif
	}
}
