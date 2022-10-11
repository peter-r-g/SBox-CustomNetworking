using System;
using System.Collections;
using System.Collections.Generic;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="List{T}"/>.
/// </summary>
/// <typeparam name="T">The type contained in the <see cref="List{T}"/>.</typeparam>
public sealed class NetworkedList<T> : INetworkable, IEnumerable<T> where T : INetworkable
{
	/// <summary>
	/// The underlying <see cref="List{T}"/> being contained inside.
	/// </summary>
	public List<T> Value
	{
		get => _value;
		set
		{
			_value = value;

			_changes.Clear();
			_changes.Add( (ListChangeType.Clear, default) );
			foreach ( var val in value )
				_changes.Add( (ListChangeType.Add, val) );
		}
	}
	private List<T> _value;

	/// <summary>
	/// Gets the total number of elements the internal data structure can hold without resizing.
	/// <returns>The number of elements that the <see cref="List{T}"/> can contain before resizing is required.</returns>
	/// </summary>
	public int Capacity => Value.Capacity;
	/// <summary>
	/// Gets the number of elements contained in the <see cref="List{T}"/>.
	/// <returns>The number of elements contained in the <see cref="List{T}"/>.</returns>
	/// </summary>
	public int Count => Value.Count;

	private readonly List<(ListChangeType, T?)> _changes = new();

	public NetworkedList( List<T> list )
	{
		Value = list;
	}

	public NetworkedList()
	{
		Value = new List<T>();
	}

	/// <summary>
	/// Adds an object to the end of the <see cref="List{T}"/>.
	/// </summary>
	/// <param name="item">The object to be added to the end of the <see cref="List{T}"/>. The value can be null for reference types.</param>
	public void Add( T item )
	{
		Value.Add( item );
		_changes.Add( (ListChangeType.Add, item) );
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
	}

	/// <summary>
	/// Removes all elements from the <see cref="List{T}"/>.
	/// </summary>
	public void Clear()
	{
		Value.Clear();
		_changes.Clear();
		_changes.Add( (ListChangeType.Clear, default) );
	}

	public IEnumerator<T> GetEnumerator()
	{
		return Value.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public bool Changed()
	{
		return _changes.Count > 0;
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
					throw new ArgumentOutOfRangeException( nameof( action ) );
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

	/// <summary>
	/// Represents a type of change the <see cref="NetworkedList{T}"/> has gone through.
	/// </summary>
	private enum ListChangeType : byte
	{
		Add,
		Remove,
		Clear
	}
}
