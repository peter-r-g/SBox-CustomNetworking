using System.Numerics;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables.Builtin;

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

	public float X => _value.X;
	public float Y => _value.Y;
	public float Z => _value.Z;
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
		throw new System.NotImplementedException();
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
		throw new System.NotImplementedException();
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
