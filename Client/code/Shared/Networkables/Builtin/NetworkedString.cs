using System;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="NetworkedString"/>.
/// </summary>
public struct NetworkedString : INetworkable<NetworkedString>, INetworkable, IEquatable<NetworkedString>
{
	public event INetworkable<NetworkedString>.ChangedEventHandler? Changed = null;
	event INetworkable<object>.ChangedEventHandler? INetworkable<object>.Changed
	{
		add => throw new InvalidOperationException();
		remove => throw new InvalidOperationException();
	}
	
	public string Value
	{
		get => _value;
		set
		{
			var oldValue = _value;
			_value = value;
			Changed?.Invoke( _value, this );
		}
	}
	private string _value;

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
		Deserialize( reader );
	}

	public void Serialize( NetworkWriter writer )
	{
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
