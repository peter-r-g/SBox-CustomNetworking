using System.IO;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

public struct NetworkedString : INetworkable
{
	public string Value
	{
		get => _value;
		set
		{
			_value = value;
			HasChanged = true;
		}
	}
	private string _value;

	public bool HasChanged { get; private set; } = false;
	public bool CanChangePartially => false;

	private NetworkedString( string s )
	{
		_value = s;
	}
	
	public void Deserialize( NetworkReader reader )
	{
		_value = reader.ReadString();
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
	
	public static implicit operator string( NetworkedString networkedString )
	{
		return networkedString.Value;
	}
	
	public static implicit operator NetworkedString( string s )
	{
		return new NetworkedString( s );
	}
}
