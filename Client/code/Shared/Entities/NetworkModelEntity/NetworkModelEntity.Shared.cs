using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkModelEntity : NetworkEntity
{
	public NetworkedQuaternion Rotation
	{
		get => _rotation;
		set
		{
			_rotation.Changed -= OnRotationChanged;
			_rotation = value;
			value.Changed += OnRotationChanged;
			OnRotationChanged( value );
		}
	}
	private NetworkedQuaternion _rotation;
	
	public NetworkedString ModelName
	{
		get => _modelName;
		set
		{
			_modelName.Changed -= OnModelNameChanged;
			_modelName = value;
			value.Changed += OnModelNameChanged;
			OnModelNameChanged( value );
		}
	}
	private NetworkedString _modelName;

	public NetworkModelEntity( int entityId ) : base( entityId )
	{
	}
	
	protected virtual void OnRotationChanged( INetworkable networkable )
	{
#if SERVER
		TriggerNetworkingChange( nameof(Rotation) );
#endif
	}
	
	protected virtual void OnModelNameChanged( INetworkable networkable )
	{
#if CLIENT
		ModelEntity.SetModel( ((NetworkedString)networkable).Value );
#endif
#if SERVER
		TriggerNetworkingChange( nameof(ModelName) );
#endif
	}
}
