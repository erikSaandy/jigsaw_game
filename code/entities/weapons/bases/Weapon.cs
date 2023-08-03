﻿using Sandbox;
using System.Collections.Generic;

namespace Jigsaw;
public partial class Weapon : Carriable
{
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
	public override string WorldModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public virtual float PrimaryAttackDelay => 0.0f;
	public virtual float SecondaryAttackDelay => 0.0f;
	public virtual float PrimaryReloadDelay => 0.0f;
	public virtual float SecondaryReloadDelay => 0.0f;
	public virtual int MaxPrimaryAmmo => 0;
	public virtual int MaxSecondaryAmmo => 0;
	public virtual AmmoType PrimaryAmmoType => AmmoType.None;
	public virtual AmmoType SecondaryAmmoType => AmmoType.None;
	public virtual bool Automatic => false;
	[Net] public int PrimaryAmmo { get; set; } = 0;
	[Net] public int SecondaryAmmo { get; set; } = 0;
	protected bool IsPrimaryReloading => TimeSincePrimaryReload < PrimaryReloadDelay;
	protected bool IsSecondaryReloading => TimeSinceSecondaryReload < SecondaryReloadDelay;
	public override void Spawn()
	{
		base.Spawn();
		PrimaryAmmo = MaxPrimaryAmmo;
	}
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
	}
	public override void Simulate( IClient cl )
	{
		if ( Owner is not JigsawPawn ) return;
		if ( CanReloadPrimary() && Input.Pressed( "Reload" ) )
		{
			TimeSincePrimaryReload = 0;
			ReloadPrimary();
		}
		if ( CanPrimaryAttack() && !IsPrimaryReloading )
		{
			TimeSincePrimaryAttack = 0;
			if ( PrimaryAmmo > 0 || MaxPrimaryAmmo == 0 )
			{
				using ( LagCompensation() )
				{
					PrimaryAttack();
				}
			}
			else
			{
				if ( CanReloadPrimary() )
				{
					TimeSincePrimaryReload = 0;
					ReloadPrimary();
				}
			}
		}
		if ( CanSecondaryAttack() && !IsSecondaryReloading )
		{
			TimeSinceSecondaryAttack = 0;
			if ( SecondaryAmmo > 0 || MaxSecondaryAmmo == 0 )
			{
				using ( LagCompensation() )
				{
					SecondaryAttack();
				}
			}
			else
			{
				if ( CanReloadSecondary() )
				{
					TimeSinceSecondaryReload = 0;
					ReloadSecondary();
				}
			}
		}
	}
	public TimeSince TimeSincePrimaryReload;
	public virtual void ReloadPrimary()
	{
		var ammo = (Owner as JigsawPawn).Ammo.AmmoCount( PrimaryAmmoType ).Clamp( 0, MaxPrimaryAmmo - PrimaryAmmo );
		(Owner as JigsawPawn).Ammo.TakeAmmo( PrimaryAmmoType, ammo );
		PrimaryAmmo += ammo;
	}
	public TimeSince TimeSinceSecondaryReload;
	public virtual void ReloadSecondary()
	{
		var ammo = (Owner as JigsawPawn).Ammo.AmmoCount( SecondaryAmmoType ).Clamp( 0, MaxSecondaryAmmo - SecondaryAmmo );
		(Owner as JigsawPawn).Ammo.TakeAmmo( SecondaryAmmoType, ammo );
		SecondaryAmmo += ammo;
	}
	public virtual bool CanReloadPrimary()
	{
		return PrimaryAmmo != MaxPrimaryAmmo && (Owner as JigsawPawn).Ammo.AmmoCount( PrimaryAmmoType ) > 0 && !IsPrimaryReloading;
	}
	public virtual bool CanReloadSecondary()
	{
		return false;
		//return Input.Pressed( InputButton.sometyhingidk ) && PrimaryAmmo != MaxPrimaryAmmo;
	}
	public TimeSince TimeSincePrimaryAttack;
	public TimeSince TimeSinceSecondaryAttack;
	public virtual void PrimaryAttack()
	{

	}
	public virtual void SecondaryAttack()
	{

	}
	public virtual bool CanPrimaryAttack()
	{
		return (Automatic ? Input.Down( "Attack1" ) : Input.Pressed( "Attack1" )) && TimeSincePrimaryAttack >= PrimaryAttackDelay;
	}
	public virtual bool CanSecondaryAttack()
	{
		return (Automatic ? Input.Down( "Attack2" ) : Input.Pressed( "Attack2" )) && TimeSinceSecondaryAttack >= SecondaryAttackDelay;
	}

	public IEnumerable<TraceResult> TraceMelee( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc", "glass" )
				.Ignore( this );

		var tr = trace.Run();

		if ( tr.Hit )
		{
			yield return tr;
		}
		else
		{
			trace = trace.Size( radius );

			tr = trace.Run();

			if ( tr.Hit )
			{
				yield return tr;
			}
		}
	}
}
