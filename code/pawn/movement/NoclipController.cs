using Sandbox;

namespace Jigsaw;

[Library]
public class NoclipController : MovementComponent
{
	public override void Simulate( IClient cl )
	{

		var fwd = Entity.InputDirection.x.Clamp( -1f, 1f );
		var left = Entity.InputDirection.y.Clamp( -1f, 1f );
		var rotation = Entity.ViewAngles.ToRotation();

		var vel = rotation.Forward * fwd + rotation.Left * left;

		if ( Input.Down( "jump" ) )
		{
			vel += Vector3.Up * 1;
		}

		vel = vel.Normal * 2000;

		if ( Input.Down( "run" ) )
			vel *= 5.0f;

		if ( Input.Down( "duck" ) )
			vel *= 0.2f;

		Entity.Velocity += vel * Time.Delta;

		if ( Entity.Velocity.LengthSquared > 0.01f )
		{
			Entity.Position += Entity.Velocity * Time.Delta;
		}

		Entity.Velocity = Entity.Velocity.Approach( 0, Entity.Velocity.Length * Time.Delta * 5.0f );

		Entity.EyeRotation = rotation;
		WishVelocity = Entity.Velocity;
		Entity.GroundEntity = null;
		Entity.BaseVelocity = Vector3.Zero;

		Entity.Tags.Add( "noclip" );
	}

}
