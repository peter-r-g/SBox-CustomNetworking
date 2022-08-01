using CustomNetworking.Shared.Utility;
#if SERVER
using System.Numerics;
#endif

namespace CustomNetworking.Shared.Networkables.Builtin;

/// <summary>
/// Represents a networkable <see cref="System.Numerics.Vector3"/>.
/// </summary>
public struct NetworkedVector3 : INetworkable
{
	public event INetworkable.ChangedEventHandler? Changed = null;
	
#if SERVER
	public Vector3 Value
#endif
#if CLIENT
	public System.Numerics.Vector3 Value
#endif
	{
		get => _value;
		set
		{
			_value = value;
			Changed?.Invoke( this );
		}
	}
#if SERVER
	private Vector3 _value;
#endif
#if CLIENT
	private System.Numerics.Vector3 _value;
#endif

	/// <summary>
	/// The <see cref="System.Numerics.Vector3.X"/> component of the <see cref="System.Numerics.Vector3"/>.
	/// </summary>
	public float X => _value.X;
	/// <summary>
	/// The <see cref="System.Numerics.Vector3.Y"/> component of the <see cref="System.Numerics.Vector3"/>.
	/// </summary>
	public float Y => _value.Y;
	/// <summary>
	/// The <see cref="System.Numerics.Vector3.Z"/> component of the <see cref="System.Numerics.Vector3"/>.
	/// </summary>
	public float Z => _value.Z;

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
		Deserialize( reader );
	}

	public void Serialize( NetworkWriter writer )
	{
		writer.Write( _value.X );
		writer.Write( _value.Y );
		writer.Write( _value.Z );
	}

	public void SerializeChanges( NetworkWriter writer )
	{
		Serialize( writer );
	}

	public override string ToString()
	{
		return Value.ToString();
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
