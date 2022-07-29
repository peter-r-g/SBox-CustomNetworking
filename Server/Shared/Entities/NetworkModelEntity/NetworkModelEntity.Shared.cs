using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkModelEntity : NetworkEntity
{
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
