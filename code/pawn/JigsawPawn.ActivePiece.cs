using Sandbox;
using System;
using System.Linq;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	private readonly string[] CollisionTags = { "default", "solid", "player" };

	[Net, Predicted] public PuzzlePiece ActivePiece { get; set; } = null;

	private readonly float SmoothTime = 1f;
	private Vector3 DampVelocity = Vector3.Zero;
	[Net, Predicted] private Vector3 PositionOld { get; set; } = Vector3.Zero;
	[Net, Predicted] private Vector3 PositionNew { get; set; } = Vector3.Zero;
	[Net, Predicted] public Vector3 PieceVelocity { get; set; } = Vector3.Zero;
	[Net, Predicted] public Angles HeldAngleOffset { get; set; } = Angles.Zero;
	[Net, Predicted] public Vector3 HeldOffset { get; set; } = Vector3.Zero;

	private readonly int MaxHeldDistance = 96;

	public void SimulateActivePiece( IClient cl )
	{

		if ( ActivePiece == null ) return;

		if ( ActivePiece.TimeSincePickedUp > 0.5f )
		{
			if ( ActivePiece.TryConnecting() )
			{
				return;
			}
		}

		Rotation rot = (EyeRotation.Angles().WithPitch( 0 ) + HeldAngleOffset).ToRotation();
		Vector3 velocity = GetWantedVelocity();

		PositionOld = ActivePiece.Position;
		PositionNew = Vector3.SmoothDamp( ActivePiece.Position, ActivePiece.Position + velocity, ref DampVelocity, 0.5f, Time.Delta );
		velocity = PositionNew - PositionOld;

		if ( Game.IsServer )
		{
			ActivePiece.Velocity = velocity * 100;
			ActivePiece.Rotation = rot;
		}

	}

	private Vector3 GetWantedVelocity()
	{
		// The distance from eye to active piece expressed as a percentage of MaxHeldDistance.
		//float pOfDst = (ActivePiece.Position - EyePosition).Length / MaxHeldDistance;

		Vector3 start = EyePosition;

		// Trace from active piece out in the eye direction until we reach MaxHeldDistance from the player eye.
		TraceResult trace = Trace.Body( ActivePiece.PhysicsBody,
			new Transform( start, ActivePiece.Rotation ), // START
			EyePosition + (EyeRotation.Forward * MaxHeldDistance) ) // END
				.WithAnyTags( CollisionTags )
				.Ignore( ActivePiece, true )
				.Ignore( this )
				.Run();

		DebugOverlay.Sphere( trace.EndPosition, 5, Color.Green );

		return trace.EndPosition - ActivePiece.Position + HeldOffset + (Vector3.Up * JigsawGame.PieceThickness / 2 * JigsawGame.PieceScale);

	}

	private void GetCollisions(Rotation rot, ref Vector3 vel)
	{
		PuzzlePiece[] group = ActivePiece.GetGroupPieces().Where( x => x.IsValid ).ToArray();

		foreach ( PuzzlePiece piece in group )
		{
			TraceResult trace = Trace.Sweep( piece.PhysicsBody, 
				new Transform(piece.PhysicsBody.Position, piece.PhysicsBody.Rotation ), // FROM
				new Transform( piece.PhysicsBody.Position + vel, rot ) ) // TO
				.WithAnyTags( CollisionTags )
				.Ignore( ActivePiece, true )
				.Run();

			if ( trace.Hit )
			{
				Vector3 v = trace.Direction.Normal * trace.Distance;

				// We need to check each axis of the vector to know how far to move in each direction.
				//This vector's X is smaller than any other vector so far (i.e first collision)
				if ( MathF.Abs( v.x ) < MathF.Abs( vel.x ) )
				{
					vel.x = v.x;
				}

				if ( MathF.Abs( v.y ) < MathF.Abs( vel.y ) )
				{
					vel.y = v.y;
				}

				if ( MathF.Abs( v.z ) < MathF.Abs( vel.z ) )
				{
					vel.z = v.z;
				}
			}
		}
	}

	private void ActivePieceInput()
	{

		if ( Input.StopProcessing )
			return;

		if ( Input.Pressed( "attack1" ))
		{

			TraceResult tr = Trace.Ray(EyePosition, EyePosition + (EyeRotation.Forward * MaxHeldDistance) )
				.UseHitboxes()
				.WithTag("puzzlepiece")
				.Ignore(this)
				.Run();

			if (tr.Hit)
			{
				PuzzlePiece root = (tr.Entity as PuzzlePiece).GetRoot();
				SetActivePiece( root, tr.HitPosition );
			}
		}

		// Throw piece
		else if(Input.Released("attack1"))
		{
			if ( ActivePiece != null )
			{
				//DebugOverlay.Line( ActivePiece.Position, ActivePiece.Position + ActivePiece.Position - PositionOld, Color.Red, 10 );

				//ActivePiece.PhysicsBody.Velocity = ( ActivePiece.Position - PositionOld ) * 20;
				ClearActivePiece();
				return;
			}
		}

	}


	private void SetActivePiece( PuzzlePiece piece, Vector3 hitPosition )
	{
		if ( Game.IsClient ) return;

		piece.Owner = this;
		ActivePiece = piece;
		piece.HeldBy = this;

		piece.TimeSincePickedUp = 0;

		piece.EnableGroupPhysics( false );

		PositionOld = ActivePiece.Position;

		HeldOffset = piece.Position - hitPosition;

		Angles _new = (ActivePiece.Rotation.Angles() - EyeRotation.Angles()).WithPitch( 0 );
		HeldAngleOffset = _new;

	}

	private void ClearActivePiece()
	{
		if ( Game.IsServer )
		{
			ActivePiece.EnableGroupPhysics( true );
		}

		ActivePiece.HeldBy = null;
		ActivePiece.Owner = null;

		ActivePiece = null;

	}

}
