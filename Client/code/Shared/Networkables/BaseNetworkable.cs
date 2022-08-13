using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables;

public abstract class BaseNetworkable : INetworkable<BaseNetworkable>, INetworkable
{
	public event INetworkable<BaseNetworkable>.ChangedEventHandler? Changed;
	event INetworkable<object>.ChangedEventHandler? INetworkable<object>.Changed
	{
		add => Logging.Fatal( new InvalidOperationException() );
		remove => Logging.Fatal( new InvalidOperationException() );
	}
	
	/// <summary>
	/// A <see cref="PropertyInfo"/> cache of all networked properties.
	/// </summary>
	protected readonly Dictionary<string, PropertyInfo> PropertyNameCache = new();
	/// <summary>
	/// A <see cref="HashSet{T}"/> of networked properties that have been changed.
	/// </summary>
	protected readonly HashSet<string> ChangedProperties = new();

	protected BaseNetworkable()
	{
		foreach ( var property in GetType().GetProperties()
			         .Where( property => property.PropertyType.IsAssignableTo( typeof(INetworkable) ) ) )
			PropertyNameCache.Add( property.Name, property );
	}
	
	/// <summary>
	/// Marks a property as changed and invokes the <see cref="Changed"/> event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected virtual void TriggerNetworkingChange( [CallerMemberName] string propertyName = "" )
	{
		if ( !PropertyNameCache.ContainsKey( propertyName ) )
			throw new InvalidOperationException( $"\"{propertyName}\" is not a networkable property on {GetType().Name}." );
		
		ChangedProperties.Add( propertyName );
		Changed?.Invoke( this, this );
	}

	public virtual void Deserialize( NetworkReader reader )
	{
		foreach ( var property in PropertyNameCache.Values )
			property.SetValue( this, reader.ReadNetworkable() );
	}

	public virtual void DeserializeChanges( NetworkReader reader )
	{
		var changedCount = reader.ReadInt32();
		for ( var i = 0; i < changedCount; i++ )
		{
			var property = PropertyNameCache[reader.ReadString()];
			
			var currentValue = property.GetValue( this );
			(currentValue as INetworkable)!.DeserializeChanges( reader );
			property.SetValue( this, currentValue );
			TriggerNetworkingChange( property.Name );
		}
	}
	
	public virtual void Serialize( NetworkWriter writer )
	{
		foreach ( var property in PropertyNameCache.Values )
			writer.WriteNetworkable( (INetworkable)property.GetValue( this )! );
	}

	public virtual void SerializeChanges( NetworkWriter writer )
	{
		writer.Write( ChangedProperties.Count );
		foreach ( var propertyName in ChangedProperties )
		{
			writer.Write( propertyName );
			writer.WriteNetworkableChanges( (INetworkable)PropertyNameCache[propertyName].GetValue( this )! );
		}
		ChangedProperties.Clear();
	}
}