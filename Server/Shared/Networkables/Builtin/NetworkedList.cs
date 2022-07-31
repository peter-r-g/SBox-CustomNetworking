using System.Collections.Generic;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="List{T}"/>.
/// </summary>
/// <typeparam name="T">The type contained in the <see cref="List{T}"/>.</typeparam>
public class NetworkedList<T> : INetworkable where T : INetworkable
{
	public event INetworkable.ChangedEventHandler? Changed;
	
	public List<T> Value
	{
		get => _value;
		set
		{
			_value = value;
			Changed?.Invoke( this );
		}
	}

	private List<T> _value;

	private NetworkedList( List<T> list )
	{
		_value = list;
	}

	/// <summary>
	/// Adds an object to the end of the <see cref="List{T}"/>.
	/// </summary>
	/// <param name="item">The object to be added to the end of the <see cref="List{T}"/>. The value can be null for reference types.</param>
	public void Add( T item )
	{
		Value.Add( item );
		Changed?.Invoke( this );
	}

	/// <summary>
	/// Removes the first occurrence of a specific object from the <see cref="List{T}"/>.
	/// </summary>
	/// <param name="item">The object to remove from the <see cref="List{T}"/>. The value can be null for reference types.</param>
	public void Remove( T item )
	{
		Value.Remove( item );
		Changed?.Invoke( this );
	}

	/// <summary>
	/// Removes all elements from the <see cref="List{T}"/>.
	/// </summary>
	public void Clear()
	{
		Value.Clear();
		Changed?.Invoke( this );
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
		throw new System.NotImplementedException();
	}

	public void Serialize( NetworkWriter writer )
	{
		writer.Write( Value.Count );
		foreach ( var item in Value )
			writer.WriteNetworkable( item );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		throw new System.NotImplementedException();
	}
	
	public static implicit operator List<T>( NetworkedList<T> networkedList )
	{
		return networkedList.Value;
	}

	public static implicit operator NetworkedList<T>( List<T> list )
	{
		return new NetworkedList<T>( list );
	}
}
