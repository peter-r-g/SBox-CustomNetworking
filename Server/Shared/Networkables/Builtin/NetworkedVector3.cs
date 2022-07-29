using System.IO;
using CustomNetworking.Shared.Utility;
#if SERVER
using System.Numerics;
#endif

namespace CustomNetworking.Shared.Networkables.Builtin;

public struct NetworkedVector3 : INetworkable
{
	public System.Numerics.Vector3 Value
	{
		get => _value;
		set
		{
			_value = value;
			HasChanged = true;
		}
	}
	private System.Numerics.Vector3 _value;

	public float X => _value.X;
	public float Y => _value.Y;
	public float Z => _value.Z;

	public bool HasChanged { get; private set; } = false;
	public bool CanChangePartially => false;

	private NetworkedVector3( Vector3 vector3 )
	{
		_value = vector3;
	}
	
	public void Deserialize( NetworkReader reader )
	{
		_value = new Vector3( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );
	}

	public void DeserializeChanges( NetworkReader reader )
	{
		throw new System.NotImplementedException();
	}

	public void Serialize( NetworkWriter writer )
	{
		writer.Write( _value.X );
		writer.Write( _value.Y );
		writer.Write( _value.Z );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		throw new System.NotImplementedException();
	}
	
	public static implicit operator Vector3( NetworkedVector3 networkedVector3 )
	{
		return networkedVector3.Value;
	}
	
	public static implicit operator NetworkedVector3( Vector3 vector3 )
	{
		return new NetworkedVector3( vector3 );
	}
}
