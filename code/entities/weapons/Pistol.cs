﻿using Sandbox;

namespace Jigsaw;

public class Pistol : Gun
{
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
	public override string WorldModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public override float PrimaryAttackDelay => 0.1f;
	public override float PrimaryReloadDelay => 3.0f;
	public override int MaxPrimaryAmmo => 17;
	public override AmmoType PrimaryAmmoType => AmmoType.Pistol;
	public override void PrimaryAttack()
	{
		PrimaryAmmo -= 1;
		ShootBullet( 10, 0.02f );
		PlaySound( "rust_pistol.shoot" );
		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
		if ( Game.IsClient )
		{
			ShootEffects();
			DoViewPunch( 1f );
		}
	}
	public override void ReloadPrimary()
	{
		base.ReloadPrimary();
		ViewModelEntity?.SetAnimParameter( "reload", true );
	}
}
