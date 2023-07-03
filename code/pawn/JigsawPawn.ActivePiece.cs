using Saandy;
using Sandbox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	[Net]
	public PuzzlePiece ActivePiece { get; set; } = null;

	private readonly int ActivePieceRotationStep = 30;

	private float SmoothTime = 1f;
	private Vector3 dampVelocity = Vector3.Zero;
	private Vector3 ActivePiecePositionOld = Vector3.Zero;

	public void SimulateActivePiece( IClient cl )
	{

		if ( Game.IsClient ) return;

		PuzzlePieceInput();

		if ( ActivePiece == null ) return;

		#region Active Piece Position

		Vector3 pWanted = GetWantedPosition();

		ActivePiecePositionOld = ActivePiece.Position;
		ActivePiece.Position = Vector3.SmoothDamp( ActivePiece.Position, pWanted, ref dampVelocity, SmoothTime, Time.Delta * 200 );

		//ActivePiece.Position = Math2d.Lerp( ActivePiece.Position, pWanted, Time.Delta * 10 );
		//ActivePiece.Position = EyePosition + (EyeRotation.Forward * 64); // + (EyeRotation.Right * 64);
		//ActivePiece.Position = pWanted;

		//float velY = ActivePiece.Velocity.y;
		//float velZ = ActivePiece.Velocity.z;
		//float X = Math2d.SmoothDamp( ActivePiece.Position.x, pWanted.x, ref smoothVelocityX, SmoothTime );
		//float Y = Math2d.SmoothDamp( ActivePiece.Position.y, pWanted.y, ref smoothVelocityY, SmoothTime );
		//float Z = Math2d.SmoothDamp( ActivePiece.Position.z, pWanted.z, ref smoothVelocityZ, SmoothTime );

		//ActivePiece.Position = new Vector3(X, Y, Z); 
		//Math2d.Lerp( ActivePiece.Position, pWanted, Time.Delta);
		//ActivePiece.Position = GetWantedPosition();

		#endregion

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

	private Vector3 GetWantedPosition()
	{
		float radius = JigsawGame.PieceScale / 2;

		string[] tags = new string[3] { "solid,", "puzzlepiece", "player" };
		PuzzlePiece.GetBoundingBox( ActivePiece.X, ActivePiece.Y, out Vector3 mins, out Vector3 maxs );
		BBox bounds = new BBox( mins, maxs );

		TraceResult trace = Trace.Box( bounds, EyePosition, EyePosition + EyeRotation.Forward * 128 )
			.Ignore( ActivePiece, true )
			.Run();

		TraceResult nudge = Trace.Box( bounds, ActivePiece.Position, trace.EndPosition )
			.Ignore( ActivePiece, true )
			.Run();

		//Log.Error( trace.Entity?.Name );
		//Log.Error( nudge.Entity?.Name );

		DebugOverlay.Box( trace.EndPosition, bounds.Mins, bounds.Maxs, Color.Green );

		if ( !nudge.Hit )
		{
			return nudge.EndPosition;
		}

		DebugOverlay.Box( nudge.EndPosition, bounds.Mins, bounds.Maxs, Color.Blue );

		return trace.EndPosition;
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

		if ( Input.Pressed( "attack1" ))
		{

			TraceResult tr = Trace.Ray(EyePosition, EyePosition + (EyeRotation.Forward * rayMag) )
				.UseHitboxes()
				.WithTag("puzzlepiece")
				.Ignore(this)
				.Run();

			if (tr.Hit)
			{
				SetActivePiece( (tr.Entity as PuzzlePiece).GetRoot() );
			}
		}
		else if(Input.Released("attack1"))
		{
			if ( ActivePiece != null )
			{
				DebugOverlay.Line( ActivePiece.Position, ActivePiece.Position + ActivePiece.Position - ActivePiecePositionOld, Color.Red, 10 );


				ActivePiece.Velocity =( ActivePiece.Position - ActivePiecePositionOld ) * 20;
				ClearActivePiece();
				return;
			}
		}


	}


	private void SetActivePiece( PuzzlePiece piece )
	{
		if ( Game.IsServer )
		{
			piece.Owner = this;
			ActivePiece = piece;

			//piece.PhysicsEnabled = false;
			//piece.UsePhysicsCollision = false;
			//piece.EnableAllCollisions = false;
			//piece.EnableTraceAndQueries = true;

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

				EnableGroupPhysics( piece, false );

				ActivePiecePositionOld = ActivePiece.Position;
			}
		}

	}

	private void EnableGroupPhysics(PuzzlePiece root, bool enable = true)
	{
		PuzzlePiece[] group = root.GetGroupPieces().Where( x => x.IsValid ).ToArray();
		foreach ( PuzzlePiece e in group )
		{
			PhysicsBody[] bodies = e.PhysicsGroup.Bodies.ToArray();
			foreach ( PhysicsBody b in bodies )
			{
				b.GravityEnabled = enable;
			}
		}
	}

	private void ClearActivePiece()
	{
		if ( Game.IsServer )
		{
			EnableGroupPhysics( ActivePiece, true );
		}

		ActivePiece.UsePhysicsCollision = true;
		ActivePiece.EnableAllCollisions = true;

		//ActivePiece.Owner = null;
		ActivePiece = null;
	}

}
