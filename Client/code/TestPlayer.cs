using Sandbox;

namespace CustomNetworking.Client;

public class TestPlayer : Player
{
	/// <summary>
	/// The grubs movement controller.
	/// </summary>
	public BasePlayerController Controller { get; private set; } = null!;
	
	/// <summary>
	/// The camera that the team client will see the game through.
	/// </summary>
	public CameraMode Camera
	{
		get => Components.Get<CameraMode>();
		private set => Components.Add( value );
	}
	
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
		Camera = new FirstPersonCamera();
		Controller = new WalkController();

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public override void Simulate( Sandbox.Client cl )
	{
		base.Simulate( cl );
		
		Controller.Simulate( cl, this, null );

		if ( Input.Pressed( InputButton.View ) )
			Camera = Camera is FirstPersonCamera ? new ThirdPersonCamera() : new FirstPersonCamera();
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
