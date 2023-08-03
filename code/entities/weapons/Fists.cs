using Saandy;
using Sandbox;
using System;

namespace Jigsaw;

partial class Fists : Weapon
{
	public override string ViewModelPath => "models/first_person/jigsaw_first_person_arms.vmdl";
	public override string WorldModelPath => "";

	public override float PrimaryAttackDelay => 0.9f;
	public override float SecondaryAttackDelay => 0.9f;

	bool TKActive = false;

	public override bool CanReloadPrimary()
	{
		return false;
	}

	private void Attack( bool leftHand )
	{
		//return; // no attack. lol

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

	public override void OnDrop( Entity dropper )
	{
		base.OnDrop(dropper);
	}

	public override void Simulate( IClient cl )
	{
		if ( Owner is not JigsawPawn ) return;
		if ( Input.StopProcessing ) return;

		JigsawPawn pawn = Owner as JigsawPawn;

		// TK

		if ( Input.Pressed( "attack1" ) )
		{

			TraceResult tr = Trace.Ray( pawn.EyePosition, pawn.EyePosition + (pawn.EyeRotation.Forward * JigsawPawn.MaxHeldDistance) )
				.UseHitboxes()
				.WithTag( "puzzlepiece" )
				.Ignore( this )
				.Run();

			if ( tr.Hit )
			{
				PuzzlePiece root = (tr.Entity as PuzzlePiece).GetRoot();
				PieceManager.SetActivePiece( cl, root, tr.HitPosition );
				EnableTK();
			}
		}
		else if ( TKActive && Input.Down( "attack1" ) )
		{
			SimulateTK( cl );
		}
		else
		{
			PieceManager.ClearActivePiece( cl );
			EnableTK( false );
		}

		base.Simulate( cl );

	}

	public override bool CanPrimaryAttack()
	{
		return (Automatic ? Input.Down( "Attack1" ) : Input.Pressed( "Attack1" )) && TimeSincePrimaryAttack >= PrimaryAttackDelay && !TKActive;
	}

	public override void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		anim.Handedness = CitizenAnimationHelper.Hand.Both;
		anim.AimBodyWeight = 1.0f;
	}

	public void EnableTK(bool enable = true)
	{
		ViewModelEntity?.SetAnimParameter( "b_tk", enable );
		TKActive = enable;
	}

	public override void CreateViewModel()
	{
		Game.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new ViewModel
		{
			Position = Position,
			Owner = Owner,
			EnableViewmodelRendering = true,
			ShouldBob = false,
		};

		ViewModelEntity.SetModel( ViewModelPath );
		ViewModelEntity.SetAnimGraph( "models/first_person/jigsaw_first_person_arms_punching.vanmgrph" );
	}

	private bool MeleeAttack()
	{

		var ray = Owner.AimRay;

		var forward = ray.Forward;
		forward = forward.Normal;

		bool hit = false;

		foreach ( var tr in TraceMelee( ray.Position, ray.Position + forward * 80, 20.0f ) )
		{
			if ( !tr.Entity.IsValid() ) continue;

			tr.Surface.DoBulletImpact( tr );

			hit = true;

			if ( !Game.IsServer ) continue;

			using ( Prediction.Off() )
			{
				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100, 25 )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );
			}
		}

		return hit;
	}

	[ClientRpc]
	private void OnMeleeMiss( bool leftHand )
	{
		Game.AssertClient();

		ViewModelEntity?.SetAnimParameter( "b_attack_has_hit", false );
		ViewModelEntity?.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "holdtype_attack", leftHand ? 2 : 1 );
	}

	[ClientRpc]
	private void OnMeleeHit( bool leftHand )
	{
		Game.AssertClient();

		ViewModelEntity?.SetAnimParameter( "b_attack_has_hit", true );
		ViewModelEntity?.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "holdtype_attack", leftHand ? 2 : 1 );
	}
}
