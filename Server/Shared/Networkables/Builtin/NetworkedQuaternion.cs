using System;
using System.Numerics;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="Quaternion"/>.
/// </summary>
public struct NetworkedQuaternion : INetworkable<NetworkedQuaternion>, INetworkable
{
	public event INetworkable<NetworkedQuaternion>.ChangedEventHandler? Changed = null;
	event INetworkable<object>.ChangedEventHandler? INetworkable<object>.Changed
	{
		add => throw new InvalidOperationException();
		remove => throw new InvalidOperationException();
	}
	
	public Quaternion Value
	{
		get => _value;
		set
		{
			var oldValue = _value;
			_value = value;
			Changed?.Invoke( oldValue, this );
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

	public override string ToString()
	{
		return Value.ToString();
	}
	
#if CLIENT
	public static implicit operator Rotation( NetworkedQuaternion networkedQuaternion )
	{
		return networkedQuaternion.Value;
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
