using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkModelEntity : NetworkEntity
{
	public NetworkedQuaternion Rotation
	{
		get => _rotation;
		set
		{
			_rotation = value;
#if SERVER
			TriggerNetworkingChange( nameof(Rotation) );
#endif
		}
	}
	private NetworkedQuaternion _rotation;
	
	public NetworkedString ModelName
	{
		get => _modelName;
		set
		{
			_modelName = value;
#if CLIENT
			ModelEntity.SetModel( value );
#endif
#if SERVER
			TriggerNetworkingChange( nameof(ModelName) );
#endif
		}
	}
	private NetworkedString _modelName = string.Empty;

	public NetworkModelEntity( int entityId ) : base( entityId )
	{
	}
}
