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
	[Net, Predicted] private Angles HeldAngleOffset { get; set; } = Angles.Zero;
	[Net, Predicted] public Vector3 HeldOffset { get; set; } = Vector3.Zero;

	private readonly int MaxHeldDistance = 96;

	public void SimulateActivePiece( IClient cl )
	{

		PuzzlePieceInput();

		if ( ActivePiece == null ) return;

		Rotation rot = (EyeRotation.Angles().WithPitch( 0 ) + HeldAngleOffset).ToRotation();
		Vector3 velocity = GetWantedVelocity();

		PositionOld = ActivePiece.Position;
		PositionNew = Vector3.SmoothDamp( ActivePiece.Position, ActivePiece.Position + velocity, ref DampVelocity, 0.5f, Time.Delta );
		velocity = PositionNew - PositionOld;

		if ( Game.IsClient ) return;

		ActivePiece.Velocity = velocity * 100;
		ActivePiece.Rotation = rot;

		if ( ActivePiece.TimeSincePickedUp > 0.5f )
		{
			ActivePiece.CheckForConnections();
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

		return trace.EndPosition - ActivePiece.Position + HeldOffset;

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

	//private Vector3 GetWantedPosition()
	//{

	//	float radius = JigsawGame.PieceScale / 2;

	//	PuzzlePiece[] group = ActivePiece.GetGroupPieces().Where( x => x.IsValid ).ToArray();


	//	Vector3 maxDst = (EyeRotation.Forward * MaxHeldDistance);
	//	Vector3 wanted = Vector3.Zero;
	//	// percent of max distance

	//	string[] tags = { "default", "solid", "player" };

	//	float pOfMaxDst = (ActivePiece.Position - EyePosition).Length / MaxHeldDistance;

	//	foreach ( PuzzlePiece piece in group )
	//	{
	//		//TODO: BUG: When ActivePiece.position goes behind EyePosition, pieces can move through walls.

	//		Vector3 mins, maxs = Vector3.Zero;
	//		PuzzlePiece.GetBoundingBox( piece.X, piece.Y, out mins, out maxs );
	//		BBox box = new BBox( mins, maxs ).Rotate( piece.Rotation );

	//		Vector3 start = piece.Transform.Position + (EyeRotation.Forward * (MaxHeldDistance * pOfMaxDst));

	//		TraceResult trace = Trace.Box( box, start, start + (EyeRotation.Forward * (MaxHeldDistance)) )
	//			.WithAnyTags( tags )
	//			.Ignore( ActivePiece, true )
	//			.Run();

	//		DebugOverlay.Box( trace.EndPosition, piece.Rotation, mins, maxs, Color.Green );

	//		if ( trace.Hit )
	//		{
	//			Vector3 v = trace.Direction.Normal * trace.Distance;

	//			// We need to check each axis of the vector to know how far to move in each direction.
	//			//This vector's X is smaller than any other vector so far (i.e first collision)
	//			if ( MathF.Abs( v.x ) < MathF.Abs( maxDst.x ) )
	//			{
	//				maxDst.x = v.x;
	//				wanted.x = ActivePiece.Position.x + trace.EndPosition.x - piece.Position.x;
	//			}

	//			if ( MathF.Abs( v.y ) < MathF.Abs( maxDst.y ) )
	//			{
	//				maxDst.y = v.y;
	//				wanted.y = ActivePiece.Position.y + trace.EndPosition.y - piece.Position.y;
	//			}

	//			if ( MathF.Abs( v.z ) < MathF.Abs( maxDst.z ) )
	//			{
	//				maxDst.z = v.z;
	//				wanted.z = ActivePiece.Position.z + trace.EndPosition.z - piece.Position.z;
	//			}
	//		}
	//	}

	//	return EyePosition + maxDst + (Vector3.Up * 3);

	//}

	private void PuzzlePieceInput()
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

		// ROTATE PIECE
		//if(Input.Down("use"))
		//{
		//	Angles a = new Angles( 0, -Input.MouseDelta.x * Time.Delta * 4, 0 );
		//	HeldAngleOffset += a;
		//}


	}


	private void SetActivePiece( PuzzlePiece piece, Vector3 hitPosition )
	{
		if ( Game.IsClient ) return;

		piece.Owner = this;
		ActivePiece = piece;
		piece.HeldBy = this;

		piece.TimeSincePickedUp = 0;

		EnableGroupPhysics( piece, false );

		PositionOld = ActivePiece.Position;

		HeldOffset = piece.Position - hitPosition;

		Angles _new = (ActivePiece.Rotation.Angles() - EyeRotation.Angles()).WithPitch( 0 );
		HeldAngleOffset = _new;

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

			//e.PhysicsEnabled = enable;
			e.UsePhysicsCollision = true;
			e.EnableAllCollisions = true;
			e.EnableTraceAndQueries = true;
			e.EnableSolidCollisions = true;
		}
	}

	private void ClearActivePiece()
	{
		if ( Game.IsServer )
		{
			EnableGroupPhysics( ActivePiece, true );
		}

		ActivePiece.HeldBy = null;
		ActivePiece.Owner = null;

		ActivePiece = null;

	}

}
