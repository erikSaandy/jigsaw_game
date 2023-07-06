using Sandbox;

namespace Jigsaw;

public class CitizenAnimationComponent : AnimationComponent
{
	Entity lastWeapon;
	public override void Simulate( IClient cl )
	{
		Log.Error( "Simulate Animator" );

		// where should we be rotated to
		var turnSpeed = 0.02f;

		Rotation rotation = Entity.ViewAngles.ToRotation();

		var idealRotation = Rotation.LookAt( rotation.Forward.WithZ( 0 ), Vector3.Up );
		Entity.Rotation = Rotation.Slerp( Entity.Rotation, idealRotation, Entity.MovementController.WishVelocity.Length * Time.Delta * turnSpeed );
		Entity.Rotation = Entity.Rotation.Clamp( idealRotation, 45.0f, out var shuffle ); // lock facing to within 45 degrees of look direction

		CitizenAnimationHelper animHelper = new CitizenAnimationHelper( Entity );

		animHelper.WithWishVelocity( Entity.MovementController.WishVelocity / Entity.Scale );
		animHelper.WithVelocity( Entity.Velocity / Entity.Scale );
		animHelper.WithLookAt( Entity.EyePosition + Entity.ViewAngles.Forward * 100.0f, 1.0f, 1.0f, 0.5f );
		animHelper.AimAngle = rotation;
		animHelper.FootShuffle = shuffle;
		animHelper.DuckLevel = MathX.Lerp( animHelper.DuckLevel, Entity.MovementController.HasTag( "ducked" ) ? 1 : 0, Time.Delta * 10.0f );
		animHelper.VoiceLevel = (Game.IsClient && Entity.Client.IsValid()) ? Entity.Client.Voice.LastHeard < 0.5f ? Entity.Client.Voice.CurrentLevel : 0.0f : 0.0f;
		animHelper.IsGrounded = Entity.GroundEntity != null;
		animHelper.IsSitting = Entity.MovementController.HasTag( "sitting" );
		animHelper.IsNoclipping = Entity.MovementController.HasTag( "noclip" );
		animHelper.IsClimbing = Entity.MovementController.HasTag( "climbing" );
		animHelper.IsSwimming = Entity.GetWaterLevel() >= 0.5f;
		animHelper.IsWeaponLowered = false;

		if ( Entity.MovementController.HasEvent( "jump" ) ) animHelper.TriggerJump();

		//if ( Entity.Inventory?.ActiveChild != lastWeapon ) animHelper.TriggerDeploy();

		//if ( Entity.Inventory?.ActiveChild is Carriable carry )
		//{
		//	carry.SimulateAnimator( animHelper );
		//}
		//else
		//{
		//	animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		//	animHelper.AimBodyWeight = 0.5f;

		//}
		//
		//lastWeapon = Entity.Inventory?.ActiveChild;
	}
}
