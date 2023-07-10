﻿using Sandbox;
using System.Linq;

namespace Jigsaw;
/// <summary>
///  Something that can go into the player's inventory and have a worldmodel and viewmodel etc, 
/// </summary>
public class Carriable : AnimatedEntity
{

	/// <summary>
	/// Utility - return the entity we should be spawning particles from etc
	/// </summary>
	public virtual ModelEntity EffectEntity => (ViewModelEntity.IsValid() && IsFirstPersonMode) ? ViewModelEntity : this;
	public Entity Carrier { get; set; }
	public virtual string WorldModelPath => null;
	public virtual string ViewModelPath => null;
	public BaseViewModel ViewModelEntity { get; protected set; }
	public override void Spawn()
	{
		base.Spawn();
		CarriableSpawn();
	}
	internal virtual void CarriableSpawn()
	{
		SetModel( WorldModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		EnableTouch = true;
	}
	/// <summary>
	/// Create the viewmodel. You can override this in your base classes if you want
	/// to create a certain viewmodel entity.
	/// </summary>
	public virtual void CreateViewModel()
	{
		Game.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new BaseViewModel();
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.SetModel( ViewModelPath );
	}

	/// <summary>
	/// We're done with the viewmodel - delete it
	/// </summary>
	public virtual void DestroyViewModel()
	{
		ViewModelEntity?.Delete();
		ViewModelEntity = null;
	}
	public override void StartTouch( Entity other )
	{
		base.Touch( other );
		if ( other is JigsawPawn ply )
		{
			if ( ply.Inventory?.Items.Where( x => x.GetType() == this.GetType() ).Count() <= 0 )
			{

				ply.Inventory?.AddItem( this );
			}
		}
	}
	public virtual void OnPickup( Entity equipper )
	{
		SetParent( equipper, true );
		Owner = equipper;
		PhysicsEnabled = false;
		EnableAllCollisions = false;
		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		if ( IsLocalPawn )
		{
			DestroyViewModel();
			CreateViewModel();
		}
	}
	public virtual void OnDrop( Entity dropper )
	{
		SetParent( null );
		Owner = null;
		PhysicsEnabled = true;
		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = false;
		EnableShadowInFirstPerson = false;

		if ( Game.IsClient )
		{
			DestroyViewModel();
		}
	}
	public virtual void OnActiveStart()
	{
		EnableDrawing = true;
		if ( Game.IsClient )
		{
			DestroyViewModel();
			CreateViewModel();
		}
	}
	public virtual void OnActiveEnd()
	{
		if ( Parent is Player ) EnableDrawing = false;
		if ( Game.IsClient )
		{
			DestroyViewModel();
		}
	}
	public virtual void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.HoldType = CitizenAnimationHelper.HoldTypes.Pistol;
		anim.Handedness = CitizenAnimationHelper.Hand.Both;
		anim.AimBodyWeight = 1.0f;
	}
}
