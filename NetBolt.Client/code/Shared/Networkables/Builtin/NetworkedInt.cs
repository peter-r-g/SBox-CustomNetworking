using System;
using NetBolt.Shared.Utility;

namespace NetBolt.Shared.Networkables.Builtin;

// TODO: In .NET 7 make this generic where T : INumber https://devblogs.microsoft.com/dotnet/dotnet-7-generic-math/
/// <summary>
/// Represents a networkable <see cref="int"/>.
/// </summary>
public struct NetworkedInt : INetworkable, IEquatable<NetworkedInt>
{
	/// <summary>
	/// The underlying <see cref="int"/> being contained inside.
	/// </summary>
	public int Value
	{
		get => _value;
		set
		{
			_oldValue = _value;
			_value = value;
		}
	}
	private int _value;
	private int _oldValue;

	private NetworkedInt( int i )
	{
		_value = i;
		_oldValue = default;
	}

	public bool Changed()
	{
		return _value != _oldValue;
	}

	public void Deserialize( NetworkReader reader )
	{
		_value = reader.ReadInt32();
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
		_oldValue = _value;
		Serialize( writer );
	}
	
	public bool Equals(NetworkedInt other)
	{
		return _value == other._value;
	}

	public override bool Equals(object? obj)
	{
		return obj is NetworkedInt other && Equals(other);
	}

	public override int GetHashCode()
	{
		return _value;
	}

	public override string ToString()
	{
		return Value.ToString();
	}

	public static NetworkedInt operator +( NetworkedInt operand ) => operand;
	public static NetworkedInt operator +( NetworkedInt left, NetworkedInt right ) => left.Value + right.Value;
	public static NetworkedInt operator ++( NetworkedInt operand ) => operand.Value + 1;
	public static NetworkedInt operator -( NetworkedInt operand ) => -operand.Value;
	public static NetworkedInt operator -( NetworkedInt left, NetworkedInt right ) => left.Value - right.Value;
	public static NetworkedInt operator --( NetworkedInt operand ) => operand.Value - 1;
	public static NetworkedInt operator *( NetworkedInt left, NetworkedInt right ) => left.Value * right.Value;
	public static NetworkedInt operator /( NetworkedInt left, NetworkedInt right ) => left.Value / right.Value;
	public static NetworkedInt operator %( NetworkedInt left, NetworkedInt right ) => left.Value % right.Value;

	public static bool operator ==( NetworkedInt left, NetworkedInt right ) => left.Value == right.Value;
	public static bool operator !=( NetworkedInt left, NetworkedInt right ) => !(left == right);
	public static bool operator <( NetworkedInt left, NetworkedInt right ) => left.Value < right.Value;
	public static bool operator >( NetworkedInt left, NetworkedInt right ) => left.Value > right.Value;
	public static bool operator <=( NetworkedInt left, NetworkedInt right ) => left.Value <= right.Value;
	public static bool operator >=( NetworkedInt left, NetworkedInt right ) => left.Value >= right.Value;
	
	public static implicit operator int( NetworkedInt networkedInt )
	{
		return networkedInt.Value;
	}

	public static implicit operator NetworkedInt( int i )
	{
		return new NetworkedInt( i );
	}
}
