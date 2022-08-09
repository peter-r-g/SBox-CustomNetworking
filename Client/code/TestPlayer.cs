using Sandbox;

namespace CustomNetworking.Client;

public class TestPlayer : Player
{
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();

		//
		// Use a watermelon model
		//
		SetModel( "models/sbox_props/watermelon/watermelon.vmdl" );
		CameraMode = new FirstPersonCamera();
		Controller = new WalkController();

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public override void Simulate( Sandbox.Client cl )
	{
		base.Simulate( cl );

		if ( Input.Pressed( InputButton.View ) )
			CameraMode = CameraMode is FirstPersonCamera ? new ThirdPersonCamera() : new FirstPersonCamera();
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( Sandbox.Client cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = Input.Rotation;
		EyeRotation = Rotation;
	}
}
