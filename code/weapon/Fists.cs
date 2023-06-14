using Sandbox;

namespace Jigsaw;

[Library( "weapon_fists", Title = "Fists" )]
partial class Fists : Weapon
{

	public override string ViewModelPath => "models/first_person/first_person_arms.vmdl";

	private void Attack( bool leftHand )
	{
		if ( MeleeAttack() )
		{
			OnMeleeHit( leftHand );
		}
		else
		{
			OnMeleeMiss( leftHand );
		}

		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
	}

	public override void PrimaryAttack()
	{
		Attack( true );
	}

	public override void SecondaryAttack()
	{
		Attack( false );
	}

	//public override void SimulateAnimator( CitizenAnimationHelper anim )
	//{
	//	anim.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
	//	anim.Handedness = CitizenAnimationHelper.Hand.Both;
	//	anim.AimBodyWeight = 1.0f;
	//}

	protected override void Animate()
	{
		Pawn.SetAnimParameter( "holdtype", (int)CitizenAnimationHelper.HoldTypes.Punch );
	}	

	private bool MeleeAttack()
	{
		//var ray = Owner.AimRay;

		//var forward = ray.Forward;
		//forward = forward.Normal;

		//bool hit = false;

		//foreach ( var tr in TraceMelee( ray.Position, ray.Position + forward * 80, 20.0f ) )
		//{
		//	if ( !tr.Entity.IsValid() ) continue;

		//	tr.Surface.DoBulletImpact( tr );

		//	hit = true;

		//	if ( !Game.IsServer ) continue;

		//	using ( Prediction.Off() )
		//	{
		//		var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100, 25 )
		//			.UsingTraceResult( tr )
		//			.WithAttacker( Owner )
		//			.WithWeapon( this );

		//		tr.Entity.TakeDamage( damageInfo );
		//	}
		//}

		//return hit;

		return false;
	}

	[ClientRpc]
	private void OnMeleeMiss( bool leftHand )
	{
		Game.AssertClient();

		ViewModelEntity?.SetAnimParameter( "attack_has_hit", false );
		ViewModelEntity?.SetAnimParameter( "attack", true );
		ViewModelEntity?.SetAnimParameter( "holdtype_attack", leftHand ? 2 : 1 );
	}

	[ClientRpc]
	private void OnMeleeHit( bool leftHand )
	{
		Game.AssertClient();

		ViewModelEntity?.SetAnimParameter( "attack_has_hit", true );
		ViewModelEntity?.SetAnimParameter( "attack", true );
		ViewModelEntity?.SetAnimParameter( "holdtype_attack", leftHand ? 2 : 1 );
	}
}
