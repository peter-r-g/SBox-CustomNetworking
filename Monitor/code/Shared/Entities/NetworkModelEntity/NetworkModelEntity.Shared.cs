using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

/// <summary>
/// A <see cref="NetworkEntity"/> that can have a model.
/// </summary>
public partial class NetworkModelEntity : NetworkEntity
{
	/// <summary>
	/// The world rotation of the <see cref="NetworkModelEntity"/>.
	/// </summary>
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
	
	/// <summary>
	/// The model the <see cref="NetworkModelEntity"/> should use.
	/// </summary>
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
	
	/// <summary>
	/// Called when <see cref="Rotation"/> has changed.
	/// </summary>
	/// <param name="networkable">The new instance of <see cref="Rotation"/>.</param>
	protected virtual void OnRotationChanged( INetworkable networkable )
	{
#if SERVER
		TriggerNetworkingChange( nameof(Rotation) );
#endif
	}
	
	/// <summary>
	/// Called when <see cref="ModelName"/> has changed.
	/// </summary>
	/// <param name="networkable">The new instance of <see cref="ModelName"/>.</param>
	protected virtual void OnModelNameChanged( INetworkable networkable )
	{
#if CLIENT && !MONITOR
		ModelEntity.SetModel( ((NetworkedString)networkable).Value );
#endif
#if SERVER
		TriggerNetworkingChange( nameof(ModelName) );
#endif
	}
}
