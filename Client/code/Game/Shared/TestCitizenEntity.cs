using System;
using System.Numerics;
using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Game;

public class TestCitizenEntity : NetworkModelEntity
{
	public TestCitizenEntity( int entityId ) : base( entityId )
	{
		Position = new Vector3( Random.Shared.NextSingle() * 100, Random.Shared.NextSingle() * 100,
			Random.Shared.NextSingle() * 100 );
		ModelName = "models/citizen/citizen.vmdl";
	}

#if SERVER
	protected override void UpdateServer()
	{
		base.UpdateServer();
		
		Position = new Vector3( Random.Shared.NextSingle() * 20, Random.Shared.NextSingle() * 20,
			Random.Shared.NextSingle() * 20 );
	}
#endif
}
