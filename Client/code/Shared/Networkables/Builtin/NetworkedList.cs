using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="List{T}"/>.
/// </summary>
/// <typeparam name="T">The type contained in the <see cref="List{T}"/>.</typeparam>
public class NetworkedList<T> : INetworkable<NetworkedList<T>>, INetworkable where T : INetworkable<T>
{
	public event INetworkable<NetworkedList<T>>.ChangedEventHandler? Changed;
	event INetworkable<object>.ChangedEventHandler? INetworkable<object>.Changed
	{
		add => throw new InvalidOperationException();
		remove => throw new InvalidOperationException();
	}
	
	public List<T> Value
	{
		get => _value;
		set
		{
			_value = value;
			Changed?.Invoke( this, this );
		}
	}
	private List<T> _value;

	public int Capacity => Value.Capacity;
	public int Count => Value.Count;

	private readonly List<(ListChangeType, T?)> _changes = new();

	public NetworkedList( List<T> list )
	{
		_value = list;
	}

	public NetworkedList()
	{
		_value = new List<T>();
	}

	/// <summary>
	/// Adds an object to the end of the <see cref="List{T}"/>.
	/// </summary>
	/// <param name="item">The object to be added to the end of the <see cref="List{T}"/>. The value can be null for reference types.</param>
	public void Add( T item )
	{
		Value.Add( item );
		_changes.Add( (ListChangeType.Add, item) );
		Changed?.Invoke( this, this );
	}

	/// <summary>
	/// Determines whether an element is in the <see cref="List{T}"/>.
	/// </summary>
	/// <param name="item">The object to locate in the <see cref="List{T}"/>. The value can be null for reference types.</param>
	/// <returns>true if item is found in the <see cref="List{T}"/>; otherwise, false.</returns>
	public bool Contains( T item )
	{
		return Value.Contains( item );
	}

	/// <summary>
	/// Removes the first occurrence of a specific object from the <see cref="List{T}"/>.
	/// </summary>
	/// <param name="item">The object to remove from the <see cref="List{T}"/>. The value can be null for reference types.</param>
	public void Remove( T item )
	{
		Value.Remove( item );
		_changes.Add( (ListChangeType.Remove, item) );
		Changed?.Invoke( this, this );
	}

	/// <summary>
	/// Removes all elements from the <see cref="List{T}"/>.
	/// </summary>
	public void Clear()
	{
		Value.Clear();
		_changes.Clear();
		_changes.Add( (ListChangeType.Clear, default) );
		Changed?.Invoke( this, this );
	}

	public void Deserialize( NetworkReader reader )
	{
		Value = new List<T>();
		var listLength = reader.ReadInt32();
		for ( var i = 0; i < listLength; i++ )
			Value.Add( reader.ReadNetworkable<T>() );
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		var changeCount = reader.ReadInt32();
		for ( var i = 0; i < changeCount; i++ )
		{
			var action = (ListChangeType)reader.ReadByte();
			T? value = default;
			if ( reader.ReadBoolean() )
				value = reader.ReadNetworkable<T>();
			
			switch ( action )
			{
				case ListChangeType.Add:
					Add( value! );
					break;
				case ListChangeType.Remove:
					Remove( value! );
					break;
				case ListChangeType.Clear:
					Clear();
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof(action) );
			}
		}
	}

	public void Serialize( NetworkWriter writer )
	{
		writer.Write( Value.Count );
		foreach ( var item in Value )
			writer.WriteNetworkable( item );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		writer.Write( _changes.Count );
		foreach ( var change in _changes )
		{
			writer.Write( (byte)change.Item1 );
			var isNull = change.Item2 is null;
			writer.Write( isNull );
			
			if ( !isNull )
				writer.WriteNetworkable( change.Item2! );
		}
		_changes.Clear();
	}
	
	public static implicit operator List<T>( NetworkedList<T> networkedList )
	{
		return networkedList.Value;
	}

	public static implicit operator NetworkedList<T>( List<T> list )
	{
		return new NetworkedList<T>( list );
	}

	private enum ListChangeType : byte
	{
		Add,
		Remove,
		Clear
	}
}
