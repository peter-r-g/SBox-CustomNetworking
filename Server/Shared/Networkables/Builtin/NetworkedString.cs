using System;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="NetworkedString"/>.
/// </summary>
public struct NetworkedString : INetworkable, IEquatable<NetworkedString>
{
	/// <summary>
	/// The underlying <see cref="string"/> contained inside.
	/// </summary>
	public string Value
	{
		get => _value;
		set
		{
			_oldValue = _value;
			_value = value;
		}
	}
	private string _value;
	private string _oldValue;

	private NetworkedString( string s )
	{
		_value = s;
		_oldValue = string.Empty;
	}

	public bool Changed()
	{
		return _value != _oldValue;
	}

	public void Deserialize( NetworkReader reader )
	{
		_value = reader.ReadString();
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		Deserialize( reader );
	}

	public void Serialize( NetworkWriter writer )
	{
		_oldValue = _value;
		writer.Write( _value );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		Serialize( writer );
	}
	
	public bool Equals(NetworkedString other)
	{
		return _value == other._value;
	}

	public override bool Equals(object? obj)
	{
		return obj is NetworkedString other && Equals(other);
	}

	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	public override string ToString()
	{
		return Value;
	}

	public static NetworkedString operator +( NetworkedString left, NetworkedString right ) => left.Value + right.Value;

	public static bool operator ==( NetworkedString left, NetworkedString right ) => left.Value == right.Value;
	public static bool operator !=( NetworkedString left, NetworkedString right ) => !(left == right);

	public static implicit operator string( NetworkedString networkedString )
	{
		return networkedString.Value;
	}
	
	public static implicit operator NetworkedString( string s )
	{
		return new NetworkedString( s );
	}
}
