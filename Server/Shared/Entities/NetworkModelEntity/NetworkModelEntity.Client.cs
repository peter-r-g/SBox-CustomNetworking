#if CLIENT && !MONITOR
using Sandbox;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkModelEntity
{
	/// <summary>
	/// The S&box model entity that will show the model.
	/// </summary>
	protected readonly ModelEntity ModelEntity = new();

	public override void Delete()
	{
		ModelEntity.Delete();
	}
	
	protected override void UpdateClient()
	{
		ModelEntity.Position = Position;
		ModelEntity.Rotation = Rotation.Value;
	}
}
#endif
