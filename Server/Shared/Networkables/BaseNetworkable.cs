using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNetworking.Shared.Utility;
#if CLIENT
using Sandbox;
#endif

namespace CustomNetworking.Shared.Networkables;

/// <summary>
/// Base class for a networkable that contains other <see cref="INetworkable"/>s.
/// </summary>
public abstract class BaseNetworkable : INetworkable
{
#if SERVER
	/// <summary>
	/// A <see cref="PropertyInfo"/> cache of all networked properties.
	/// </summary>
	protected readonly Dictionary<string, PropertyInfo> PropertyNameCache = new();
#endif
#if CLIENT
	/// <summary>
	/// A <see cref="PropertyInfo"/> cache of all networked properties.
	/// </summary>
	protected readonly Dictionary<string, PropertyDescription> PropertyNameCache = new();
#endif

	protected BaseNetworkable()
	{
		foreach ( var property in TypeHelper.GetAllProperties( GetType() )
			         .Where( property => property.PropertyType.IsAssignableTo( typeof(INetworkable) ) ) )
			PropertyNameCache.Add( property.Name, property );
	}

	public bool Changed()
	{
		foreach ( var propertyInfo in PropertyNameCache.Values )
		{
			// TODO: handle null values.
			if ( propertyInfo.GetValue( this ) is not INetworkable networkable )
				return false;

			if ( networkable.Changed() )
				return true;
		}
		
		return false;
	}

	public virtual void Deserialize( NetworkReader reader )
	{
		foreach ( var propertyInfo in PropertyNameCache.Values )
			propertyInfo.SetValue( this, reader.ReadNetworkable() );
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
		}
	}
	
	public virtual void Serialize( NetworkWriter writer )
	{
		foreach ( var propertyInfo in PropertyNameCache.Values )
			writer.WriteNetworkable( (INetworkable)propertyInfo.GetValue( this )! );
	}

	public virtual void SerializeChanges( NetworkWriter writer )
	{
		var numChanged = 0;
		var changedStreamPos = writer.BaseStream.Position;
		writer.BaseStream.Position += sizeof(int);
		
		foreach ( var (propertyName, propertyInfo) in PropertyNameCache )
		{
			var networkable = (INetworkable)propertyInfo.GetValue( this )!;
			if ( !networkable.Changed() )
				continue;

			numChanged++;
			writer.Write( propertyName );
			writer.WriteNetworkableChanges( ref networkable );
			if ( TypeHelper.IsStruct( propertyInfo.PropertyType ) )
				propertyInfo.SetValue( this, networkable );
		}

		var tempPos = writer.BaseStream.Position;
		writer.BaseStream.Position = changedStreamPos;
		writer.Write( numChanged );
		writer.BaseStream.Position = tempPos;
	}
}
