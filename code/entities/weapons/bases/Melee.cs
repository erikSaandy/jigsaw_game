﻿using Sandbox;

namespace Jigsaw;
public partial class Melee : Weapon
{
	public virtual void AttackEffects()
	{
		Game.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

	}
	public virtual void Attack( float damage = 0, float distance = 64, float spread = 0, float force = 0 )
	{
		Game.SetRandomSeed( Time.Tick );
		var forward = Owner.AimRay.Forward;
		forward += Vector3.Random * spread;
		var tr = Trace.Ray( Owner.AimRay.Position, Owner.AimRay.Position + (forward * distance) ).UseHitboxes().Ignore( Owner ).WithoutTags( "trigger" ).Run();
		if ( tr.Hit )
		{
			tr.Surface.DoBulletImpact( tr );
			if ( tr.Entity.IsValid() )
			{
				tr.Entity.TakeDamage( DamageInfo.FromBullet( tr.HitPosition, forward * force, damage ).WithWeapon( this ).WithAttacker( Owner ) );
			}
		}
	}
}
