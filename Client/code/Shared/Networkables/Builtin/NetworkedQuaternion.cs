using System;
using System.Numerics;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="Quaternion"/>.
/// </summary>
public struct NetworkedQuaternion : INetworkable, IEquatable<NetworkedQuaternion>
{
	public event EventHandler? Changed = null;
	
	/// <summary>
	/// The underlying <see cref="Quaternion"/> contained inside.
	/// </summary>
	public Quaternion Value
	{
		get => _value;
		set
		{
			var oldValue = _value;
			_value = value;
			Changed?.Invoke( oldValue, EventArgs.Empty );
		}
	}
	private Quaternion _value;

	/// <summary>
	/// The <see cref="Quaternion.X"/> component of the <see cref="Quaternion"/>.
	/// </summary>
	public float X => _value.X;
	/// <summary>
	/// The <see cref="Quaternion.Y"/> component of the <see cref="Quaternion"/>.
	/// </summary>
	public float Y => _value.Y;
	/// <summary>
	/// The <see cref="Quaternion.Z"/> component of the <see cref="Quaternion"/>.
	/// </summary>
	public float Z => _value.Z;
	/// <summary>
	/// The <see cref="Quaternion.W"/> component of the <see cref="Quaternion"/>.
	/// </summary>
	public float W => _value.W;

	private NetworkedQuaternion( Quaternion quaternion )
	{
		_value = quaternion;
	}

	public void Deserialize( NetworkReader reader )
	{
		_value = new Quaternion( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		Deserialize( reader );
	}

	public void Serialize( NetworkWriter writer )
	{
		writer.Write( X );
		writer.Write( Y );
		writer.Write( Z );
		writer.Write( W );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		Serialize( writer );
	}

	public bool Equals( NetworkedQuaternion other )
	{
		return _value.Equals( other._value );
	}

	public override bool Equals( object? obj )
	{
		return obj is NetworkedVector3 other && Equals( other );
	}

	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString();
	}
	
	public static bool operator ==( NetworkedQuaternion left, NetworkedQuaternion right ) => left.Equals( right );
	public static bool operator !=( NetworkedQuaternion left, NetworkedQuaternion right ) => !(left == right);
	
#if CLIENT
	public static implicit operator Rotation( NetworkedQuaternion networkedQuaternion )
	{
		return networkedQuaternion.Value;
	}

	public static implicit operator NetworkedQuaternion( Rotation rotation )
	{
		return new NetworkedQuaternion( new Quaternion( rotation.x, rotation.y, rotation.z, rotation.w ) );
	}
#endif

	public static implicit operator Quaternion( NetworkedQuaternion networkedQuaternion )
	{
		return networkedQuaternion.Value;
	}
	
	public static implicit operator NetworkedQuaternion( Quaternion quaternion )
	{
		return new NetworkedQuaternion( quaternion );
	}
}
