using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNetworking.Shared.Utility;
#if CLIENT
using Sandbox;
using Logging = CustomNetworking.Shared.Utility.Logging;
#endif

namespace CustomNetworking.Shared.Networkables;

/// <summary>
/// Base class for a networkable that contains other <see cref="INetworkable"/>s.
/// </summary>
public abstract class BaseNetworkable : INetworkable
{
	/// <summary>
	/// The unique identifier of the networkable.
	/// </summary>
	public int NetworkId { get; }

	/// <summary>
	/// An internal map of <see cref="BaseNetworkable"/> identifiers that were not accessible at the time and need setting after de-serializing all <see cref="BaseNetworkable"/>s.
	/// </summary>
	internal readonly Dictionary<int, string> ClPendingNetworkables = new();
	
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

	protected BaseNetworkable( int networkId )
	{
		NetworkId = networkId;
		
		foreach ( var property in TypeHelper.GetAllProperties( GetType() )
			         .Where( property => property.PropertyType.IsAssignableTo( typeof(INetworkable) ) ) )
			PropertyNameCache.Add( property.Name, property );

		AllNetworkables.Add( NetworkId, this );
	}

	/// <summary>
	/// Deletes the <see cref="BaseNetworkable"/>. You should not be using this after calling this.
	/// </summary>
	public virtual void Delete()
	{
		// TODO: Notify client of this
		AllNetworkables.Remove( NetworkId );
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
		var count = reader.ReadInt32();
		for ( var i = 0; i < count; i++ )
		{
			var propertyName = reader.ReadString();
			var propertyInfo = PropertyNameCache[propertyName];
			if ( propertyInfo.PropertyType.IsAssignableTo( typeof(BaseNetworkable) ) )
			{
				var networkId = reader.ReadInt32();
				if ( All.TryGetValue( networkId, out var networkable ) )
					propertyInfo.SetValue( this, networkable );
				else
					ClPendingNetworkables.Add( networkId, propertyName );
			}
			else
				propertyInfo.SetValue( this, reader.ReadNetworkable() );
		}
	}

	public virtual void DeserializeChanges( NetworkReader reader )
	{
		var changedCount = reader.ReadInt32();
		for ( var i = 0; i < changedCount; i++ )
		{
			var propertyName = reader.ReadString();
			var property = PropertyNameCache[propertyName];
			if ( property.PropertyType.IsAssignableTo( typeof(BaseNetworkable) ) )
			{
				var networkId = reader.ReadInt32();
				if ( All.TryGetValue( networkId, out var networkable ) )
					property.SetValue( this, networkable );
				else
					ClPendingNetworkables.Add( networkId, propertyName );
			}
			else
			{
				var currentValue = property.GetValue( this );
				(currentValue as INetworkable)!.DeserializeChanges( reader );
				property.SetValue( this, currentValue );
			}
		}
	}
	
	public virtual void Serialize( NetworkWriter writer )
	{
		writer.Write( PropertyNameCache.Count );
		foreach ( var (propertyName, propertyInfo) in PropertyNameCache )
		{
			writer.Write( propertyName );
			var networkable = (INetworkable)propertyInfo.GetValue( this )!;
			if ( propertyInfo.PropertyType.IsAssignableTo( typeof(BaseNetworkable) ) &&
			     networkable is BaseNetworkable baseNetworkable )
				writer.Write( baseNetworkable.NetworkId );
			else
				writer.WriteNetworkable( networkable );
		}
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
			if ( networkable is BaseNetworkable baseNetworkable )
				writer.Write( baseNetworkable.NetworkId );
			else
			{
				writer.WriteNetworkableChanges( ref networkable );
				if ( TypeHelper.IsStruct( propertyInfo.PropertyType ) )
					propertyInfo.SetValue( this, networkable );
			}
		}

		var tempPos = writer.BaseStream.Position;
		writer.BaseStream.Position = changedStreamPos;
		writer.Write( numChanged );
		writer.BaseStream.Position = tempPos;
	}

	internal static IReadOnlyDictionary<int, BaseNetworkable> All => AllNetworkables;
	private static readonly Dictionary<int, BaseNetworkable> AllNetworkables = new();
	private static int _nextNetworkId = SharedConstants.MaxEntities + 1;

	/// <summary>
	/// Creates a new <see cref="BaseNetworkable"/>.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="BaseNetworkable"/> to create.</typeparam>
	/// <returns>The created instance of <see cref="BaseNetworkable"/>.</returns>
	public static T Create<T>() where T : BaseNetworkable
	{
		var networkable = TypeHelper.Create<T>( StepNextId() );
		if ( networkable is not null )
			return networkable;

		Logging.Error( $"Failed to create networkable type {typeof(T)}" );
		return default!;
	}

	/// <summary>
	/// Gets a new <see cref="NetworkId"/> and steps the internal counter.
	/// </summary>
	/// <returns>A unique network identifier.</returns>
	public static int StepNextId()
	{
		return _nextNetworkId++;
	}
}
