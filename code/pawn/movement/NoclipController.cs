
using Jigsaw;
using Sandbox;

namespace Jigsaw;
[Library]
public partial class NoclipController : MovementComponent
{

	[Net] public float EyeHeight { get; set; } = 64.0f;
	public override void BuildInput()
	{
		Entity.InputDirection = Input.AnalogMove;
	}
	public override void Simulate( IClient cl )
	{

		Events?.Clear();
		Tags?.Clear();

		Entity.EyeLocalPosition = Vector3.Up * EyeHeight;
		Entity.EyeRotation = Entity.ViewAngles.ToRotation();

		var fwd = Entity.InputDirection.x.Clamp( -1f, 1f );
		var left = Entity.InputDirection.y.Clamp( -1f, 1f );
		var rotation = Entity.ViewAngles.ToRotation();

		var vel = (rotation.Forward * fwd) + (rotation.Left * left);

		if ( Input.Down( "Jump" ) )
		{
			vel += Vector3.Up * 1;
		}

		vel = vel.Normal * (2000 * Entity.Scale);

		if ( Input.Down( "Run" ) )
			vel *= 5.0f;

		if ( Input.Down( "Duck" ) )
			vel *= 0.2f;

		Entity.Velocity += vel * Time.Delta;

		if ( Entity.Velocity.LengthSquared > 0.01f )
		{
			Entity.Position += Entity.Velocity * Time.Delta;
		}

		Entity.Velocity = Entity.Velocity.Approach( 0, Entity.Velocity.Length * Time.Delta * 5.0f );

		WishVelocity = Entity.Velocity;
		Entity.GroundEntity = null;
		Entity.BaseVelocity = Vector3.Zero;

		SetTag( "noclip" );
	}
}
