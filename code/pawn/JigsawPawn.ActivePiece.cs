using Saandy;
using Sandbox;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	[Net]
	public PuzzlePiece ActivePiece { get; set; } = null;

	private readonly int ActivePieceRotationStep = 30;

	public void SimulateActivePiece( IClient cl )
	{

		PuzzlePieceInput();

		if ( ActivePiece == null ) return;

		ActivePiece.Position = EyePosition + (EyeRotation.Forward * 64); // + (EyeRotation.Right * 64);

		// Rotate active piece
		if ( Input.Down( "attack2" ) && ActivePiece != null )
		{
			Vector2 deltaRot = new Vector2();
			if ( Input.Pressed( "Forward" ) ) deltaRot.y += ActivePieceRotationStep;
			if ( Input.Pressed( "Backward" ) ) deltaRot.y -= ActivePieceRotationStep;
			if ( Input.Pressed( "Right" ) ) deltaRot.x -= ActivePieceRotationStep;
			if ( Input.Pressed( "Left" ) ) deltaRot.x += ActivePieceRotationStep;

			Angles a = ActivePiece.Rotation.Angles();
			//ActivePiece.LocalRotation = new Angles( a.pitch + deltaRot.x, a.yaw + deltaRot.y, a.roll ).ToRotation();

			ActivePiece.Rotation *= (Rotation)Quaternion.CreateFromAxisAngle( Vector3.Up, (a.pitch + deltaRot.x).DegreeToRadian() );
			//ActivePiece.Rotation *= (Rotation)Quaternion.CreateFromAxisAngle( ActivePiece.Rotation.Right, (a.yaw + deltaRot.y).DegreeToRadian() );

			//ActivePiece.Rotation = new Rotation( ActivePiece.Rotation.y + deltaRot.y, ActivePiece.Rotation.x + deltaRot.x, ActivePiece.Rotation.z, ActivePiece.Rotation.w );
		}

		if(ActivePiece.TimeSincePickedUp > 0.5f ) {
			ActivePiece.CheckForConnections(); 
		}

	}

	public void BuildActivePieceInput()
	{

		if ( Input.StopProcessing )
			return;

		// Rotate active piece
		if ( !Input.Down( "attack2" ) )
		{
			InputDirection = Input.AnalogMove;
		}
		else if( Input.Pressed("attack2") )
		{
			InputDirection = Vector2.Zero;
		}

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;

	}

	private void PuzzlePieceInput()
	{
		if ( Input.StopProcessing )
			return;

		float rayMag = 256;

		if ( Game.IsClient ) return;

		if ( Input.Pressed( "use" ))
		{
			if ( ActivePiece != null )
			{
				ClearActivePiece();
				return;
			}

			TraceResult tr = Trace.Ray(EyePosition, EyePosition + (EyeRotation.Forward * rayMag) )
				.UseHitboxes()
				.WithTag("puzzlepiece")
				.Ignore(this)
				.DynamicOnly()
				.Run();

			if (tr.Hit)
			{
				SetActivePiece( (tr.Entity as PuzzlePiece).GetRoot() );
			}

		}
	}


	private void SetActivePiece( PuzzlePiece piece )
	{
		piece.Owner = this;
		ActivePiece = piece;

		//piece.PhysicsEnabled = false;
		piece.UsePhysicsCollision = false;
		piece.EnableAllCollisions = false;
		piece.Parent = this;
		piece.HeldBy = this;
		piece.TimeSincePickedUp = 0;

		if ( Game.IsServer )
		{
			Angles a = piece.LocalRotation.Angles();
			piece.LocalRotation = new Angles(
				a.pitch - (a.pitch % ActivePieceRotationStep),
				a.yaw - (a.yaw % ActivePieceRotationStep),
				a.roll - (a.roll % ActivePieceRotationStep)
				).ToRotation();
		}

	}

	private void ClearActivePiece()
	{
		ActivePiece.UsePhysicsCollision = true;
		ActivePiece.EnableAllCollisions = true;

		ActivePiece.Parent = null;

		//ActivePiece.Owner = null;
		ActivePiece = null;
	}

}
