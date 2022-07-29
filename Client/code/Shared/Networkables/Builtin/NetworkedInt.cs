using System.IO;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

public struct NetworkedInt : INetworkable
{
	public int Value
	{
		get => _value;
		set
		{
			_value = value;
			HasChanged = true;
		}
	}
	private int _value;

	public bool HasChanged { get; private set; } = false;
	public bool CanChangePartially => false;

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
