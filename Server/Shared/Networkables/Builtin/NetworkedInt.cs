using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

public struct NetworkedInt : INetworkable
{
	public event INetworkable.ChangedEventHandler? Changed = null;
	
	public int Value
	{
		get => _value;
		set
		{
			_value = value;
			Changed?.Invoke( this );
		}
	}
	private int _value;

	private NetworkedInt( int i )
	{
		_value = i;
	}

	public void Deserialize( NetworkReader reader )
	{
		_value = reader.ReadInt32();
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		throw new System.NotImplementedException();
	}

	public void Serialize( NetworkWriter writer )
	{
		writer.Write( _value );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		throw new System.NotImplementedException();
	}

	public static implicit operator int( NetworkedInt networkedInt )
	{
		return networkedInt.Value;
	}

	public static implicit operator NetworkedInt( int i )
	{
		return new NetworkedInt( i );
	}
}
