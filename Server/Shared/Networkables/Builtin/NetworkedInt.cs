using System;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

// TODO: In .NET 7 make this generic where T : INumber https://devblogs.microsoft.com/dotnet/dotnet-7-generic-math/
/// <summary>
/// Represents a networkable <see cref="int"/>.
/// </summary>
public struct NetworkedInt : INetworkable<NetworkedInt>, INetworkable, IEquatable<NetworkedInt>
{
	public event INetworkable<NetworkedInt>.ChangedEventHandler? Changed = null;
	event INetworkable<object>.ChangedEventHandler? INetworkable<object>.Changed
	{
		add => throw new InvalidOperationException();
		remove => throw new InvalidOperationException();
	}
	
	public int Value
	{
		get => _value;
		set
		{
			var oldValue = _value;
			_value = value;
			Changed?.Invoke( oldValue, this );
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
