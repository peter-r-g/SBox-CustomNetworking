using System;
using System.Numerics;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="Quaternion"/>.
/// </summary>
public struct NetworkedQuaternion : INetworkable
{
	public event INetworkable.ChangedEventHandler? Changed = null;
	
	public Quaternion Value
	{
		get => _value;
		set
		{
			_value = value;
			Changed?.Invoke( this );
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
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	public override string ToString()
	{
		return Value.ToString();
	}

	public static implicit operator Quaternion( NetworkedQuaternion networkedQuaternion )
	{
		return networkedQuaternion.Value;
	}
	
	public static implicit operator NetworkedQuaternion( Quaternion quaternion )
	{
		return new NetworkedQuaternion( quaternion );
	}
}
