using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

/// <summary>
/// A <see cref="NetworkEntity"/> that can have a model.
/// </summary>
public partial class NetworkModelEntity : NetworkEntity
{
	/// <summary>
	/// The model the <see cref="NetworkModelEntity"/> should use.
	/// </summary>
	public NetworkedString ModelName
	{
		get => _modelName;
		set
		{
			var oldModelName = _modelName;
			_modelName.Changed -= OnModelNameChanged;
			_modelName = value;
			value.Changed += OnModelNameChanged;
			OnModelNameChanged( oldModelName, value );
		}
	}
	private NetworkedString _modelName;

	public NetworkModelEntity( int entityId ) : base( entityId )
	{
	}

	/// <summary>
	/// Called when <see cref="ModelName"/> has changed.
	/// </summary>
	/// <param name="oldModelName">The old instance of <see cref="ModelName"/>.</param>
	/// <param name="newModelName">The new instance of <see cref="ModelName"/>.</param>
	protected virtual void OnModelNameChanged( NetworkedString oldModelName, NetworkedString newModelName )
	{
#if CLIENT && !MONITOR
		ModelEntity.SetModel( newModelName );
#endif
#if SERVER
		TriggerNetworkingChange( nameof(ModelName) );
#endif
	}
}
