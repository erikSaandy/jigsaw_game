﻿using Sandbox;
using System;
using System.ComponentModel;
using System.Linq;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{

	[Net] public bool TKActive { get; set; } = false;

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{

		Event.Run( "Player.PreSpawn", this );
		base.Spawn();
		Velocity = Vector3.Zero;

		Components.RemoveAll();

		LifeState = LifeState.Alive;
		Health = 100;

		SetModel( "models/citizen/jigsaw_citizen.vmdl" );
		SetAnimGraph( "models/citizen/jigsaw_citizen.vanmgrph" );

		Components.Add( new MovementController() );
		Components.Add( new FirstPersonCamera() );
		Components.Add( new AmmoStorageComponent() );
		Components.Add( new InventoryComponent() );
		Components.Add( new CitizenAnimationComponent() );
		Components.Add( new UseComponent() );
		//Components.Add( new FallDamageComponent() );
		Components.Add( new UnstuckComponent() );

		Ammo.ClearAmmo();
		//CreateHull();
		Tags.Add( "player" );

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableTouch = true;
		EnableLagCompensation = true;
		Predictable = true;
		EnableHitboxes = true;

		MoveToSpawnpoint();

		Inventory.AddItem( TypeLibrary.Create<Entity>( "Flashlight" ) );
		Inventory.AddItem( TypeLibrary.Create<Entity>( "Fists" ) );

		Event.Run( "Player.PostSpawn", this );

	}

	/// <summary>
	/// Respawn this player.
	/// </summary>
	/// 
	public virtual void Respawn()
	{
		Event.Run( "Player.PreRespawn", this );
		Spawn();
		Event.Run( "Player.PostRespawn", this );
	}

	public async void RespawnDelayed(int ms)
	{
		if ( Game.IsClient ) return;

		await Task.Delay( ms );
		Respawn();
	}

	public virtual void MoveToSpawnpoint()
	{
		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			//Transform = tx;
			Position = tx.Position;
			Rotation = tx.Rotation;

		}
	}
	// An example BuildInput method within a player's Pawn class.
	[ClientInput] public Vector3 InputDirection { get; set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public MovementComponent MovementController => Components.Get<MovementComponent>();
	public CameraComponent CameraController => Components.Get<CameraComponent>();
	public AnimationComponent AnimationController => Components.Get<AnimationComponent>();
	public InventoryComponent Inventory => Components.Get<InventoryComponent>();
	public AmmoStorageComponent Ammo => Components.Get<AmmoStorageComponent>();
	public UseComponent UseKey => Components.Get<UseComponent>();
	public UnstuckComponent UnstuckController => Components.Get<UnstuckComponent>();


	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

	public BBox Hull
	{
		get => new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 72 )
		);
	}

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );
	/// <summary>
	/// Create a physics hull for this player. The hull stops physics objects and players passing through
	/// the player. It's basically a big solid box. It also what hits triggers and stuff.
	/// The player doesn't use this hull for its movement size.
	/// </summary>

	DamageInfo LastDamage;
	public override void TakeDamage( DamageInfo info )
	{
		if ( Game.IsClient ) return;
		Event.Run( "Player.PreTakeDamage", info, this );
		LastDamage = info;
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;
		if ( Health > 0f && LifeState == LifeState.Alive )
		{
			Health -= info.Damage;
			if ( Health <= 0f )
			{
				Health = 0f;
				OnKilled();
			}
		}
		Event.Run( "Player.PostTakeDamage", info, this );
	}
	public override void OnKilled()
	{
		if ( Game.IsClient ) return;
		Event.Run( "Player.PreOnKilled", this );
		LifeState = LifeState.Dead;
		BecomeRagdoll( LastDamage );

		Inventory.ActiveChild = null;
		Inventory.ActiveChildInput = null;

		EnableAllCollisions = false;
		EnableDrawing = false;

		Inventory.DropItem( Inventory.ActiveChild );
		foreach ( var item in Inventory.Items.ToList() )
		{
			Inventory.DropItem( item );
		}

		Inventory.Items.Clear();

		Components.Add( new NoclipController() );

		Event.Run( "Player.PostOnKilled", this );

		//Respawn();
		RespawnDelayed( 5000 );

	}


	//---------------------------------------------// 

	/// <summary>
	/// Pawns get a chance to mess with the input. This is called on the client.
	/// </summary>
	public override void BuildInput()
	{
		base.BuildInput();
		// these are to be done in order and before the simulated components
		CameraController?.BuildInput();
		MovementController?.BuildInput();
		AnimationController?.BuildInput();

		foreach ( var i in Components.GetAll<SimulatedComponent>() )
		{
			if ( i.Enabled ) i.BuildInput();
		}

	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( ActivePiece == null )
		{
			if ( Input.MouseWheel > 0.1 )
			{
				Inventory?.SwitchActiveSlot( 1, true );
			}
			if ( Input.MouseWheel < -0.1 )
			{
				Inventory?.SwitchActiveSlot( -1, true );
			}
		}

		// these are to be done in order and before the simulated components
		UnstuckController?.Simulate( cl );
		MovementController?.Simulate( cl );
		CameraController?.Simulate( cl );
		AnimationController?.Simulate( cl );

		foreach ( var i in Components.GetAll<SimulatedComponent>() )
		{
			if ( i.Enabled ) i.Simulate( cl );
		}

		//SimulateActivePiece( cl );

	}

	/// <summary>
	/// Called when the Active child is detected to have changed
	/// </summary>
	public virtual void OnActiveChildChanged( Entity previous, Entity next )
	{
		if ( previous is Carriable previousC )
		{
			previousC?.OnDrop( this );
		}

		if ( next is Carriable nextC )
		{
			nextC?.OnPickup( this );
		}
	}


	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
		// these are to be done in order and before the simulated components
		UnstuckController?.FrameSimulate( cl );
		MovementController?.FrameSimulate( cl );
		CameraController?.FrameSimulate( cl );
		AnimationController?.FrameSimulate( cl );
		foreach ( var i in Components.GetAll<SimulatedComponent>() )
		{
			if ( i.Enabled ) i.FrameSimulate( cl );
		}
	}
	TimeSince timeSinceLastFootstep = 0;
	public override void OnAnimEventFootstep( Vector3 position, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( Game.IsServer )
			return;

		if ( timeSinceLastFootstep < 0.2f )
			return;
		volume *= FootstepVolume();
		var tr = Trace.Ray( position, position + Vector3.Down * 20 ).Radius( 1 ).Ignore( this ).Run();
		if ( !tr.Hit ) return;
		timeSinceLastFootstep = 0;
		tr.Surface.DoFootstep( this, tr, foot, volume * 10 );
	}

	public virtual float FootstepVolume()
	{
		if ( MovementController is MovementController wlk )
		{
			if ( wlk.IsDucking ) return 0.3f;
		}
		return 1;
	}
}
