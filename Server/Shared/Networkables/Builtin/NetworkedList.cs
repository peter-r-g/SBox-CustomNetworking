using System.Collections.Generic;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

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

	public void Add( T item )
	{
		Value.Add( item );
		Changed?.Invoke( this );
	}

	public void Remove( T item )
	{
		Value.Remove( item );
		Changed?.Invoke( this );
	}

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
