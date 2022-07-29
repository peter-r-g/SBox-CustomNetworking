#if CLIENT
using Sandbox;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkModelEntity
{
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
