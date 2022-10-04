using System;
using System.Collections;
using System.Collections.Generic;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="HashSet{T}"/>.
/// </summary>
/// <typeparam name="T">The type contained in the <see cref="HashSet{T}"/>.</typeparam>
public sealed class NetworkedHashSet<T> : INetworkable, IEnumerable<T> where T : INetworkable
{
	/// <summary>
	/// The underlying <see cref="HashSet{T}"/> being contained inside.
	/// </summary>
	public HashSet<T> Value
	{
		get => _value;
		set
		{
			_value = value;
			
			_changes.Clear();
			_changes.Add( (HashSetChangeType.Clear, default) );
			foreach ( var val in value )
				_changes.Add( (HashSetChangeType.Add, val) );
		}
	}
	private HashSet<T> _value;
	
	/// <summary>
	/// Gets the number of elements that are contained in a set.
	/// <returns>The number of elements that are contained in the set.</returns>
	/// </summary>
	public int Count => Value.Count;
	
	/// <summary>
	/// The list of changes that have happened since the last time this was networked.
	/// </summary>
	private readonly List<(HashSetChangeType, T?)> _changes = new();
	
	public NetworkedHashSet( HashSet<T> hashSet )
	{
		_value = hashSet;
	}

	public NetworkedHashSet()
	{
		_value = new HashSet<T>();
	}
	
	/// <summary>
	/// Adds the specified element to a set.
	/// </summary>
	/// <param name="item">The element to add to the set.</param>
	/// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
	public bool Add( T item )
	{
		var result = Value.Add( item );
		if ( !result )
			return false;

		_changes.Add( (HashSetChangeType.Add, item) );
		return true;
	}

	/// <summary>
	/// Determines whether a <see cref="HashSet{T}"/> object contains the specified element.
	/// </summary>
	/// <param name="item">The element to locate in the <see cref="HashSet{T}"/> object.</param>
	/// <returns>true if the <see cref="HashSet{T}"/> object contains the specified element; otherwise, false.</returns>
	public bool Contains( T item )
	{
		return Value.Contains( item );
	}

	/// <summary>
	/// Removes the specified element from a <see cref="HashSet{T}"/> object.
	/// </summary>
	/// <param name="item">The element to remove.</param>
	/// <returns>true if the element is successfully found and removed; otherwise, false. This method returns false if item is not found in the <see cref="HashSet{T}"/> object.</returns>
	public bool Remove( T item )
	{
		var result = Value.Remove( item );
		if ( !result )
			return false;
		
		_changes.Add( (HashSetChangeType.Remove, item) );
		return true;
	}

	/// <summary>
	/// Removes all elements from a <see cref="HashSet{T}"/> object.
	/// </summary>
	public void Clear()
	{
		Value.Clear();
		_changes.Clear();
		_changes.Add( (HashSetChangeType.Clear, default) );
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
		Value = new HashSet<T>();
		var listLength = reader.ReadInt32();
		for ( var i = 0; i < listLength; i++ )
			Value.Add( reader.ReadNetworkable<T>() );
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		var changeCount = reader.ReadInt32();
		for ( var i = 0; i < changeCount; i++ )
		{
			var action = (HashSetChangeType)reader.ReadByte();
			T? value = default;
			if ( reader.ReadBoolean() )
				value = reader.ReadNetworkable<T>();
			
			switch ( action )
			{
				case HashSetChangeType.Add:
					Add( value! );
					break;
				case HashSetChangeType.Remove:
					Remove( value! );
					break;
				case HashSetChangeType.Clear:
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

	/// <summary>
	/// Represents a type of change the <see cref="NetworkedHashSet{T}"/> has gone through.
	/// </summary>
	private enum HashSetChangeType : byte
	{
		Add,
		Remove,
		Clear
	}
}
