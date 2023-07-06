using Sandbox;

namespace Jigsaw;

[Library]
public class FirstPersonCamera : CameraComponent
{
	protected override void OnActivate()
	{
		base.OnActivate();
		// Set field of view to whatever the user chose in options
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
	}
	public override void FrameSimulate( IClient cl )
	{
		// Update rotation every frame, to keep things smooth  

		Entity.EyeRotation = Entity.ViewAngles.ToRotation();

		Camera.Position = Entity.EyePosition;
		Camera.Rotation = Entity.ViewAngles.ToRotation();

		Camera.Main.SetViewModelCamera( Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView ) );

		// Set the first person viewer to this, so it won't render our model
		Camera.FirstPersonViewer = Entity;

		Camera.ZNear = 8 * Entity.Scale;
	}
	public override void BuildInput()
	{
		if ( Game.LocalClient.Components.TryGet<DevCamera>( out var _ ) )
			return;

		var viewAngles = (Entity.ViewAngles + Input.AnalogLook).Normal;
		Entity.ViewAngles = viewAngles.WithPitch( viewAngles.pitch.Clamp( -89f, 89f ) );
		return;
	}
}
